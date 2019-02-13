using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Types;

namespace Ghx.RoslynScript
{
    public partial class CsxComponent
    {
        public string SourceUri { get; private set; } = null;

        /// <summary>
        /// The wrapper around the compiled script
        /// </summary>
        private CsxScript m_script;

        /// <summary>
        /// The utility class to manage the exchange data between the component and the script
        /// </summary>
        private CsxInputsSide m_inside;
        
        /// <summary>
        /// The utility class to manage the exchange data between the script and the component
        /// </summary>
        private CsxOutputsSide m_outside;

        public override void AddedToDocument(GH_Document document)
        {
            // The script can be initialized via Read ()
            if ( m_script == null)
            {
                m_script = new CsxScript();
                m_inside = new CsxInputsSide(this, m_script);
                m_outside = new CsxOutputsSide(this, m_script);
                m_script.OnUpdated += OnUpdated;
                m_script.OnError += OnError;
            }
            
            base.AddedToDocument(document);
        }

        internal void OnError (string message)
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

            m_inside.Sync();
            m_outside.Sync();
            
            Params.OnParametersChanged();
            OnDisplayExpired(true);
            ExpireSolution(true);
        }

        private delegate void FuncOnThread();
        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (SourceUri == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No script defined");
                return;
            }

            if (!m_script.IsValid)
            {
                OnError("Script is invalid");
                return;
            }

            var program = m_script.Instance;
            foreach(CsxParameter param in Params.Input)
                param.SendData(DA, program);
            
            //if (!m_inconnector.Send(DA))
            //    return;

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

                    //m_outconnector.Receive(DA);
                    var p = m_script.Instance;
                    foreach (CsxParameter param in Params.Output)
                        param.ReceiveData(DA, p);

                    Instances.ActiveCanvas.Document.NewSolution(false);
                }
            )));
        }
    }
}
