using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace Ghx.RoslynScript
{
    public interface ICsxParameter
    {
        Type TypeRhino { get; }
        Type TypeGoo   { get; }
        Type TypeHint  { get; }

        CsxField Field { get; }

        bool SendData(IGH_DataAccess DA, object program);
    }

    public class CsxParameter : Param_GenericObject
    {
        private KeyValuePair<Type, CsxMap.MapItem> m_map;

        public override Guid ComponentGuid => new Guid("{32D8D66F-1FC6-4D96-AA26-EB792A2DB2B1}");

        protected override Bitmap Icon => m_map.Value.Icon;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public Type TypeRhino => m_map.Key;

        public Type TypeGoo => m_map.Value.GhType;

        public IGH_TypeHint TypeHint => m_map.Value.GhHint;
        
        private CsxField m_field;
        
        public CsxField Field
        {
            //Param.Access = typeof(IGH_DataTree).IsAssignableFrom(type)
            //             ? GH_ParamAccess.tree
            //             : typeof(IEnumerable).IsAssignableFrom(type)
            //             ? GH_ParamAccess.list
            //             : GH_ParamAccess.item;

            get => m_field;

            set {
                if (value == null || value.Attribute == null || value.Info == null)
                    return;

                m_field = value;

                Optional = true;
                Name = value.Info.Name;
                NickName = value.Attribute.NickName;
                Description = value.Attribute.Description;

                var type = value.Info.FieldType;
                Type trh;

                if (type.IsGenericType)
                {
                    var argsT = type.GetGenericArguments();
                    if (argsT.Length == 2)
                    {
                        if (type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>)))
                            throw new NotImplementedException("Dictionary input is not implemented");

                        throw new Exception($"Can't import the data with format : {type.FullName}");
                    }

                    if (argsT.Length != 1)
                        throw new Exception($"Invalid type for input {value.Info.Name}");

                    if(type.GetGenericTypeDefinition().IsAssignableFrom(typeof(DataTree<>)))
                    {
                        Access = GH_ParamAccess.tree;
                        trh = argsT[0];
                    }
                    else if(type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
                    {
                        Access = GH_ParamAccess.list;
                        trh = argsT[0];
                    }
                    else
                        throw new Exception($"Can't import the data with format : {type.FullName}");
                }
                else if (type.IsArray)
                {
                    throw new NotImplementedException("Array input is not implemented");
                }
                else
                {
                    Access = GH_ParamAccess.item;
                    trh = type;
                }

                m_map = CsxMap.FromRh(trh ?? typeof(object));
            }
        }

        public CsxParameter()
        {
            base.Name = "ScriptData";
            base.NickName = "var";
            base.Description = "Contains a collection of script-friendly data";
            base.Category = "Maths";
            base.SubCategory = "Script";
            Access = GH_ParamAccess.item;
            m_map = CsxMap.FromRh(typeof(object));
        }

        protected override void CollectVolatileData_FromSources()
        {
            base.CollectVolatileData_FromSources();
        }

        public override void PostProcessData()
        {
            base.PostProcessData();
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();
        }
        
        internal bool SendData(IGH_DataAccess DA, object program)
        {
            try
            {
                ClearRuntimeMessages();
                
                if (VolatileDataCount == 0)
                {
                    var attr = m_field.Attribute;
                    m_field.Info.SetValue(program, attr.DefaultValue);
                    return true;
                }
                
                switch (Access)
                {
                    case GH_ParamAccess.item:

                        IGH_Goo val = null;
                        DA.GetData(Name, ref val);

                        var rval = TypeCast(val);
                        m_field.Info.SetValue(program, rval);

                        break;

                    case GH_ParamAccess.list:

                        var list = new List<IGH_Goo>();
                        DA.GetDataList(Name, list);

                        var rlist = (IList)Activator.CreateInstance(m_field.Info.FieldType); // List<object>
                        foreach (var item in list)
                            rlist.Add(TypeCast(item));
                        m_field.Info.SetValue(program, rlist);

                        break;

                    case GH_ParamAccess.tree:

                        var DataTreeT = typeof(DataTree<>); // TODO make static
                        var targetT = DataTreeT.MakeGenericType(new[] { TypeRhino });
                        var AddRangeM = targetT.GetMethods().Single(m => m.Name == "AddRange" && m.GetParameters().Count() == 2);

                        var st = new GH_Structure<IGH_Goo>();
                        DA.GetDataTree(Name, out st);
                        
                        var rtree = Activator.CreateInstance(targetT);
                        checked
                        {
                            var j = 0;
                            foreach (var branch in st.Branches)
                            {
                                var path = st.get_Path(j);
                                var rbranch = new List<object>();
                                foreach (var item in branch)
                                    rbranch.Add(TypeCast(item));
                                AddRangeM.Invoke(rtree, new object[] { rbranch, path });
                                ++j;
                            }
                            m_field.Info.SetValue(program, rtree);
                        }

                        break;
                    }

                return true;

                object TypeCast(IGH_Goo data)
                {
                    if (data == null)
                        return null;

                    if (TypeHint == null)
                        return data.ScriptVariable();

                    object objectValue = data.ScriptVariable();
                    TypeHint.Cast(objectValue, out object result);
                    return result;
                }
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                return false;
            }
        }
        
        internal void ReceiveData(IGH_DataAccess DA, object program)
        {
            ExpireSolution(false);

            var data = Field.Info.GetValue(program);
            if (data == null)
                return;

            // if(field.Attribute.Convert != null)
            //     data = field.Attribute.Convert(data);

            var goo = Activator.CreateInstance(TypeGoo) as IGH_Goo;
            goo.CastFrom(data);
            DA.SetData(Field.Info.Name, goo);
        }
    }
}
