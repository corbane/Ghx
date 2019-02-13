using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ghx.RoslynScript
{
    public interface ICsxScript
    {
        bool IsValid { get; }
        object Instance { get; }
        string Uri { get; }
        List<CsxField> Inputs { get; }
        List<CsxField> Outputs { get; }

        event CsxScript.OnUpdatedHandler OnUpdated;

        void DefineSource(string uri);
        Task<object> Run(object[] values = null);
    }

    public class CsxField
    {
        public readonly FieldInfo Info;
        public readonly ICsxAttribute Attribute;
        public readonly ICsxScript Script;
        public CsxField(FieldInfo info, ICsxAttribute attr, ICsxScript script)
        {
            Info = info;
            Attribute = attr;
            Script = script;
        }
    }

    public class CsxScript : ICsxScript
    {
        //private static Dictionary <string, Assembly> m_loadeduri = new Dictionary<string, Assembly> ();

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

        private List<CsxField> m_inputs = new List<CsxField> ();

        private List<CsxField> m_outputs = new List<CsxField> ();

        public List<CsxField> Inputs => m_inputs;

        public List<CsxField> Outputs => m_outputs;

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

            if(result.Assembly == null)
                return;

            m_assembly = null;
            m_ctor = null;
            m_entrypoint = null;
            m_program = null;
            m_inputs.Clear();
            m_outputs.Clear();

            m_assembly = result.Assembly;

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

            m_outputs.AddRange(from field in cls.GetFields()
                               let attr = field.GetCustomAttribute(typeof(Output))
                               where attr != null
                               select new CsxField (field, attr as ICsxAttribute, this));

            m_inputs.AddRange (from field in cls.GetFields()
                               let attr = field.GetCustomAttribute(typeof(Input))
                               where attr != null
                               select new CsxField (field, attr as ICsxAttribute, this));

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
