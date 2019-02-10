using System;
using System.IO;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ghx.RoslynScript
{
    public class Script
    {
        private static Dictionary <string, Assembly> m_loadeduri = new Dictionary<string, Assembly> ();

        private Assembly m_assembly;

        private object m_program;

        private ConstructorInfo m_ctor;

        private MethodInfo m_entrypoint;

        public bool IsValid => m_assembly != null && m_ctor != null && m_entrypoint != null;

        public object Instance
        {
            get {
                if (!IsValid)
                    return null;

                return m_program == null
                    ? (m_program = m_ctor.Invoke(new[] { new object[2] }))
                    : m_program;
            }
        }

        public string Uri { get; private set; }

        private FieldInfo[] m_inputs;

        private FieldInfo[] m_outputs;

        public ImmutableArray<FieldInfo> Inputs => m_inputs.ToImmutableArray();

        public ImmutableArray<FieldInfo> Outputs => m_outputs.ToImmutableArray();

        public delegate void OnUpdatedHandler();

        public event OnUpdatedHandler OnUpdated;

        public delegate void OnErrorHandler(string message);

        public event OnErrorHandler OnError;

        public void DefineSource(string uri)
        {
            Uri = uri;
            var result = LanguageService.Create(uri, true, OnSourceChanged);
            OnSourceChanged(result);
        }

        private void OnSourceChanged (LanguageService.CompilationResult result)
        {
            if (result.Error != null)
            {
                OnError?.Invoke(result.Error);
                return;
            }

            if(result.IsNewAssembly)
            {
                if(m_loadeduri.ContainsKey(Uri))
                    m_loadeduri[Uri] = Assembly.Load(File.ReadAllBytes(result.AssemblyLocation));
                else
                    m_loadeduri.Add(Uri, Assembly.Load(File.ReadAllBytes(result.AssemblyLocation)));
            }
            else if(!m_loadeduri.ContainsKey(Uri))
            {
                m_loadeduri.Add(Uri, Assembly.Load(File.ReadAllBytes(result.AssemblyLocation)));
            }
            else
            {
                return;
            }

            m_assembly = null;
            m_ctor = null;
            m_entrypoint = null;
            m_program = null;
            //m_inputs = new FieldInfo[] { };
            //m_outputs = new FieldInfo[] { };

            m_assembly = m_loadeduri[Uri];

            var cls = (from t in m_assembly.GetExportedTypes()
                           where t.IsClass && t.Name == "Program"
                           select t).FirstOrDefault();

            if (cls == null)
            {
                OnError?.Invoke("Cant not find the entry class");
                return;
            }

            m_ctor = cls.GetConstructors().FirstOrDefault();

            if (m_ctor == null)
            {
                OnError?.Invoke("Cant not find the entry program");
                return;
            }

            m_entrypoint = cls.GetTypeInfo().GetDeclaredMethod("<Initialize>");

            if (m_entrypoint == null)
            {
                OnError?.Invoke("Cant not find the entry point");
                return;
            }

            m_outputs = (from field in cls.GetFields()
                         where field.GetCustomAttribute(typeof(Output)) != null
                         select field).ToArray();

            m_inputs = (from field in cls.GetFields()
                        where field.GetCustomAttribute(typeof(Input)) != null
                        select field).ToArray();

            OnUpdated?.Invoke();
        }
        
        public Task<object> Run(object[] values = null)
        {
            if (!IsValid)
                throw new Exception("Internal error");
            
            return (Task<object>) m_entrypoint.Invoke(Instance, null);
        }
    }
}
