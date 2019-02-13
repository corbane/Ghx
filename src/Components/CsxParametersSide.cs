using GH_IO.Serialization;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ghx.RoslynScript
{
    public class CsxInputsSide : CsxParametersSide
    {
        protected override List<CsxField> m_fields => m_script.Inputs;

        protected override List<IGH_Param> m_params => m_owner.Params.Input;

        public CsxInputsSide(CsxComponent owner, ICsxScript script) : base(owner, script)
        {
            m_Sort = m_owner.Params.SortInput;
            m_Register = m_owner.Params.RegisterInputParam;
            m_UnRegister = m_owner.Params.UnregisterInputParameter;
        }
    }


    public class CsxOutputsSide : CsxParametersSide
    {
        protected override List<CsxField> m_fields => m_script.Outputs;

        protected override List<IGH_Param> m_params => m_owner.Params.Output;

        public CsxOutputsSide(CsxComponent owner, ICsxScript script) : base(owner, script)
        {
            m_Sort = m_owner.Params.SortOutput;
            m_Register = m_owner.Params.RegisterOutputParam;
            m_UnRegister = m_owner.Params.UnregisterOutputParameter;
        }
    }


    public abstract class CsxParametersSide
    {
        protected ICsxScript m_script;
        protected CsxComponent m_owner;

        protected abstract List<CsxField> m_fields { get; }
        protected abstract List<IGH_Param> m_params { get; }
        protected Func<IGH_Param, bool> m_Register;
        protected Func<IGH_Param, bool> m_UnRegister;
        protected Action<int[]> m_Sort;

        public CsxParametersSide(CsxComponent owner, ICsxScript script)
        {
            m_script = script;
            m_owner = owner;
        }

        public bool Write(GH_IWriter writer)
        {
            foreach(CsxParameter p in m_params)
                writer.SetGuid(p.Field.Info.Name, p.InstanceGuid);
            
            return true;
        }

        public bool Read(GH_IReader reader)
        {
            foreach(var item in reader.Items)
            {
                var field = FindField(item.Name);
                var param = FindParam(item._guid);

                if( field == null)
                {
                    if(param != null)
                        m_UnRegister(param);
                }
                else if(param == null)
                {
                    param = new CsxParameter();
                    m_Register(param);
                    param.Field = field;
                    continue;
                }
                else
                {
                    param.Field = field;
                }
            }

            // In case the script defines more parameters than the current block.
            // Call Sync() to add and sort them
            Sync();

            return true;

        }

        public void Sync()
        {
            var exists = new List<Guid>();

            // Update or register GH parameters
            foreach (var field in m_fields)
            {
                var param = FindParam (field.Info.Name);
                if(param != null)
                {
                    param.Field = field;
                }
                else
                {
                    param = new CsxParameter();
                    m_Register(param);
                    param.Field = field;
                }
                
                exists.Add(param.InstanceGuid);
            }

            // Remove inused GH parameters
            for (var i = m_params.Count - 1; i >= 0 ; --i)
            {
                if (!exists.Contains(m_params[i].InstanceGuid))
                    m_UnRegister(m_params[i]);
            }

            Sort();
        }

        private void Sort ()
        {
            Debug.Assert (m_fields.Count == m_params.Count,
                m_fields.Count > m_params.Count
                ? "Internal error ! There are more script fields than input parameters"
                : "Internal error ! There are more input parameters than script fields");

            var idx = new int[m_params.Count];
            for (var i = 0; i != m_params.Count; ++i)
            {
                var k = FindFieldIndex(m_params[i].Name);
                Debug.Assert (k != -1, "Internal error ! the FindFieldIndex in Sort method return -1");
                idx[i] = k;
            }

            m_Sort(idx);

            int FindFieldIndex(string name)
            {
                var j = 0;
                foreach (var field in m_fields)
                {
                    if (field.Info.Name == name)
                        return j;
                    ++j;
                }
                return -1;
            }
        }
        
        CsxField FindField(string name)
        {
            foreach (var field in m_fields)
            {
                if (field.Info.Name == name)
                    return field;
            }
            return null;
        }

        CsxParameter FindParam(Guid id)
        {
            foreach (var param in m_params)
            {
                if (param.InstanceGuid == id)
                    return param as CsxParameter;
            }
            return null;
        }

        CsxParameter FindParam(string name)
        {
            foreach (var param in m_params)
            {
                if (param.Name == name)
                    return param as CsxParameter;
            }
            return null;
        }
    }
}
