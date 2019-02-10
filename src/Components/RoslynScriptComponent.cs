using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;

namespace Ghx.RoslynScript
{
    public class GhxCSharpScriptComponent : GH_Component, IGH_VariableParameterComponent
    {
        // Call stack:
        // -- Open GH
        // Constructor
        // Constructor
        // -- GH is open
        // -- Open definition
        // Constructor
        // Read
        // AddedToDocument
        // SolveInstance
        // -- Definition is loaded

        public override Guid ComponentGuid => new Guid("005726e4-fccb-451f-a9dd-6abeeec27216");

        protected override System.Drawing.Bitmap Icon => Ghx.RoslynScript.Properties.Resources.ico_ghx_cscript;
        
        private string m_sourceuri = null;

        private Script m_script = null;

        public GhxCSharpScriptComponent() : base(
            "Ghx.CSharpScript", "CSX", "Execute external C# script file (.csx)",
            "Math", "Script"
        ) { }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("source", m_sourceuri);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            var source = reader.GetString("source");

            if (File.Exists(source))
                m_sourceuri = source;

            return base.Read(reader);
        }

        public override void AddedToDocument(GH_Document document)
        {
            VerifyEditor();

            m_script = new Script();
            m_script.OnUpdated += OnUpdated;
            m_script.OnError += OnError;

            if ( m_sourceuri != null )
                m_script.DefineSource(m_sourceuri);

            base.AddedToDocument(document);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Source...", (object sender, EventArgs e) =>
            {
                var dialog = new Rhino.UI.OpenFileDialog()
                {
                    InitialDirectory = Rhino.RhinoDoc.ActiveDoc.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                    Filter = "Roslyn CSharp Script (*.csx)|*.csx",
                    Title = "Open script"
                };

                if( !dialog.ShowOpenDialog() )
                    return;

                var path = LanguageService.ResolveScriptUri (dialog.FileName);
                if( path == null )
                {
                    ClearRuntimeMessages();
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Can not find the script ${dialog.FileName}");
                    return;
                }
                
                CodeEditor.CreateNewScriptIfNeed(path);
                m_sourceuri = path;
                m_script.DefineSource(path);
            });

            Menu_AppendItem(menu, "Debug...", (object sender, EventArgs e) => CodeEditor.ShowEditor(m_sourceuri));

            base.AppendAdditionalComponentMenuItems(menu);
        }

        private void OnError (string message)
        {
            ClearRuntimeMessages();
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
            foreach (var param in Params.Output)
            {
                param.ClearData();
                param.ExpireSolution(false);
            }
            ExpirePreview(true);
        }

        /// <summary>
        /// Called after m_script.DefineSource(path) or after saving the source file
        /// </summary>
        private void OnUpdated ()
        {
            if(!m_script.IsValid)
                return;

            // Update inputs if need

            var idx = new bool[Params.Input.Count];
            foreach (var field in m_script.Inputs)
            {
                var i = Params.IndexOfInputParam(field.Name);
                if (i != -1)
                {
                    // TODO: The field exists but th type, description 
                    // and other can change ...
                    idx[i] = true;
                    continue;
                }

                var attr = field.GetCustomAttributes(typeof(Input), true)[0] as IOAttribute;
                var param = CreateParamFrom(field, attr);

                Params.RegisterInputParam(param);
            }
            
            for( var i = idx.Length - 1; i != -1; --i )
            {
                if (!idx[i])
                    Params.UnregisterInputParameter(Params.Input[i]);
            }

            // Update outputs if need

            idx = new bool[Params.Output.Count];
            foreach (var field in m_script.Outputs)
            {
                var i = Params.IndexOfOutputParam(field.Name);
                if ( i != -1)
                {
                    // TODO: The field exists but th type, description 
                    // and other can change ...
                    idx[i] = true;
                    continue;
                }

                var attr = field.GetCustomAttributes(typeof(Output), true)[0] as IOAttribute;
                var param = CreateParamFrom(field, attr);

                Params.RegisterOutputParam(param);
            }

            for (var i = idx.Length - 1; i != -1; --i)
            {
                if (!idx[i])
                    Params.UnregisterOutputParameter(Params.Output[i]);
            }

            // TODO: if inputs are deleted

            IGH_Param CreateParamFrom(FieldInfo field, IOAttribute attr)
            {
                var param = new Grasshopper.Kernel.Parameters.Param_ScriptVariable()
                {
                    Optional = true,
                    Access = GH_ParamAccess.item,
                    Name = field.Name,
                    NickName = attr.NickName,
                    Description = attr.Description,
                    ShowHints = false
                };

                //TODO: Hints

                return param;
            }

            // Refresh the solution
            
            Params.OnParametersChanged();
            OnDisplayExpired(true);
            ExpireSolution(true);
        }

        private delegate void FuncOnThread();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (m_sourceuri == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No script defined");
                return;
            }

            if (!m_script.IsValid)
            {
                OnError("Script is invalid");
                return;
            }
            
            // Update inputs data

            foreach (var field in m_script.Inputs)
            {
                object val = new object();

                if (!DA.GetData(field.Name, ref val))
                {
                    var attr = field.GetCustomAttribute(typeof(Input)) as IOAttribute;
                    field.SetValue(m_script.Instance, attr.DefaultValue);
                    continue;
                }

                if (val != null)
                {
                    if ((val as Grasshopper.Kernel.Types.IGH_Goo).CastTo(out object o))
                        field.SetValue(m_script.Instance, o);
                }
            }

            // Execute the script

            m_script.Run().ContinueWith(task => Rhino.RhinoApp.InvokeOnUiThread(
                new FuncOnThread(() =>
                {
                    if (task.Exception != null)
                    {
                        var err = task.Exception as Exception;
                        if (err.Source == null)  // is an AggregateException
                            err = err.InnerException;

                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err.ToString());
                        Instances.ActiveCanvas.Document.NewSolution(false);
                        throw err;
                    }
                        
                    // Update outputs data

                    if (m_script.Instance == null)
                        throw new Exception("Internal error ! the Script.Object must be defined");

                    foreach (var field in m_script.Outputs)
                    {
                        var i = Params.IndexOfOutputParam(field.Name);
                        Params.Output[i].ExpireSolution(false);
                        DA.SetData(i, field.GetValue(m_script.Instance));
                    }

                    Instances.ActiveCanvas.Document.NewSolution(false);
                }
            )));
        }

        #region editor

        private bool m_has_editor = false;

        private void VerifyEditor ()
        {
            m_has_editor = CodeEditor.VSCodeExists();

            if (!m_has_editor)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Blank,
                    "Install Visual Studio Code for enable the debug mode (https://code.visualstudio.com)"
                );
            }
        }

        #endregion

        #region IGH_VariableParameterComponent

        // https://www.grasshopper3d.com/forum/topics/how-to-register-input-output-params-dynamically-c-vs

        // La simple implémentation de l'interface signifie que vos composants seront correctement (dés) sérialisés
        // Sans cette interface, le composant suppose que le constructeur configure toutes les entrées et sorties et que la lecture des fichiers ne fonctionnera plus correctement.

        // Il est également important de toujours appeler Params.OnParametersChanged lorsque vous avez terminé d’apporter des modifications.

        // Cependant, vous n'êtes jamais censé faire cela depuis RunScript ou SolveInstance.
        // Des modifications de la topologie des composants et des modifications des fils ne peuvent être effectuées que lorsqu'une solution n'est pas en cours d'exécution. 

        // Les paramètres de script prennent certaines mesures pour protéger les données pouvant être partagées entre plusieurs composants d'un fichier GH.
        // Les paramètres de script offrent également des mécanismes de transtypage aux types standard (bool, int, double, chaîne, Brep, Curve, Point3d, etc.) et aux modificateurs d'accès (item, liste, arborescence).

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return null;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return false;
        }

        public void VariableParameterMaintenance()
        {
        }

        #endregion
        
        protected override void RegisterInputParams(GH_InputParamManager pManager) { }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
    }
}
