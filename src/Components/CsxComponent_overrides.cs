using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Types;

namespace Ghx.RoslynScript
{
    public partial class CsxComponent : GH_Component, IGH_VariableParameterComponent
    {
        public override Guid ComponentGuid => new Guid("005726e4-fccb-451f-a9dd-6abeeec27216");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ico_ghx_cscript;

        public CsxComponent() : base(
            "Ghx.CSharpScript", "CSX", "Execute external C# script file (.csx)",
            "Math", "Script"
        )
        {
            Attributes = new CsxComponentAttribute(this);
        }

        #region IO

        public override bool Write(GH_IWriter writer)
        {
            var result = base.Write(writer);

            writer.SetString("source", SourceUri);
            m_inside.Write(writer.CreateChunk("m_inconnector"));
            m_outside.Write(writer.CreateChunk("m_outconnector"));

            return result;
        }

        public override bool Read(GH_IReader reader)
        {
            var result = base.Read(reader);

            m_script = new CsxScript();

            m_script.OnError += OnError;

            var source = reader.GetString("source");
            if (File.Exists(source))
            {
                SourceUri = source;
                m_script.DefineSource(SourceUri);
            }

            m_script.OnUpdated += OnUpdated;

            m_inside = new CsxInputsSide(this, m_script);
            m_outside = new CsxOutputsSide(this, m_script);

            if (reader.ChunkExists("m_inconnector"))
                m_inside.Read(reader.FindChunk("m_inconnector"));

            if (reader.ChunkExists("m_outconnector"))
                m_outside.Read(reader.FindChunk("m_outconnector"));

            return result;
        }

        #endregion

        #region Menu

        private static bool m_has_editor = CodeEditor.VSCodeExists();

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Source...", (object sender, EventArgs e) => OpenCsxSourceDialog());
            Menu_AppendItem(menu, "Debug...", (object sender, EventArgs e) => OpenCsxEditor());
        }

        public void OpenCsxSourceDialog()
        {
            var dialog = new Rhino.UI.OpenFileDialog()
            {
                InitialDirectory = Rhino.RhinoDoc.ActiveDoc.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                Filter = "Roslyn CSharp Script (*.csx)|*.csx",
                Title = "Open script"
            };

            if (!dialog.ShowOpenDialog())
                return;

            var path = LanguageService.ResolveScriptUri(dialog.FileName);
            if (path == null)
            {
                ClearRuntimeMessages();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Can not find the script ${dialog.FileName}");
                return;
            }

            CodeEditor.CreateNewScriptIfNeed(path);
            SourceUri = path;
            m_script.DefineSource(path);
        }

        public void OpenCsxEditor()
        {
            if (!m_has_editor)
            { 
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Blank,
                    "Install Visual Studio Code for enable the debug mode (https://code.visualstudio.com)"
                );
            }
            CodeEditor.ShowEditor(SourceUri);
        }

        #endregion

        #region Parameters

        protected override void RegisterInputParams(GH_InputParamManager pManager) { }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

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
    }

    internal class CsxComponentAttribute : GH_ComponentAttributes
    {
        public CsxComponentAttribute(IGH_Component owner) : base(owner) { }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var comp = (CsxComponent)Owner;
            if (comp.SourceUri == null)
                comp.OpenCsxSourceDialog();
            else
                comp.OpenCsxEditor();

            return GH_ObjectResponse.Handled;
        }
    }
}
