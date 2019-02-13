using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Ghx.RoslynScript
{
    public static class CsxMap
    {
        public struct MapItem
        {
            private string m_inoname;
            private Bitmap m_ico;
            private static Type s_ressources;

            public Type GhType;

            public IGH_TypeHint GhHint;

            public Bitmap Icon
            {
                get {
                    if (m_ico != null)
                        return m_ico;

                    if (s_ressources == null)
                        s_ressources = typeof(GH_Document).Assembly.GetType("Grasshopper.My.Resources.Res_ObjectIcons");
                    
                    var prp = s_ressources.GetProperty(m_inoname, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    m_ico = prp.GetValue(null) as Bitmap;
                    return m_ico;
                }
            }

            public MapItem(Type ghType, IGH_TypeHint ghHint, string iconame)
            {
                m_inoname = iconame;
                m_ico = null;
                GhType = ghType;
                GhHint = ghHint;
            }

            public static KeyValuePair<Type, MapItem> Default => s_map.Last();
            
        }

        public static KeyValuePair<Type, MapItem> FromRh(Type rhType)
        {
            if (s_map.ContainsKey(rhType))
                return new KeyValuePair<Type, MapItem>(rhType, s_map[rhType]);
            
            return MapItem.Default;
        }

        public static KeyValuePair<Type, MapItem> FromGh(Type ghType)
        {
            foreach (var record in s_map)
            {
                if (record.Value.GhType != ghType)
                    continue;
                
                return record;
            }

            return MapItem.Default;
        }

        private static readonly Dictionary<Type, MapItem> s_map = new Dictionary<Type, MapItem>
        {
            {typeof(bool),          new MapItem (GH_TypeLib.t_gh_bool,          new GH_BooleanHint_CS (),   "Param_Boolean_24x24")},
            {typeof(int),           new MapItem (GH_TypeLib.t_gh_int,           new GH_IntegerHint_CS(),    "Param_Integer_24x24" )},
            {typeof(double),        new MapItem (GH_TypeLib.t_gh_number,        new GH_DoubleHint_CS(),     "Param_Number_24x24" )},
            {typeof(string),        new MapItem (GH_TypeLib.t_gh_string,        new GH_StringHint_CS(),     "Param_String_24x24" )},
            {typeof(DateTime),      new MapItem (GH_TypeLib.t_gh_time,          new GH_DateTimeHint(),      "Param_Time_24x24" )},
            {typeof(Color),         new MapItem (GH_TypeLib.t_gh_colour,        new GH_ColorHint(),         "Param_Colour_24x24" )},
            {typeof(Guid),          new MapItem (GH_TypeLib.t_gh_guid,          new GH_GuidHint(),          "Param_Guid_24x24" )},
            {typeof(Point3d),       new MapItem (GH_TypeLib.t_gh_point,         new GH_Point3dHint(),       "Param_Point_24x24" )},
            {typeof(Vector3d),      new MapItem (GH_TypeLib.t_gh_vector,        new GH_Vector3dHint(),      "Param_Vector_24x24" )},
            {typeof(Plane),         new MapItem (GH_TypeLib.t_gh_plane,         new GH_PlaneHint(),         "Param_Plane_24x24" )},
            {typeof(Interval),      new MapItem (GH_TypeLib.t_gh_interval,      new GH_IntervalHint(),      "Param_Interval_24x24" )},
            {typeof(Rectangle3d),   new MapItem (GH_TypeLib.t_gh_rectangle,     new GH_Rectangle3dHint(),   "Param_Rectangle_24x24" )},
            {typeof(Box),           new MapItem (GH_TypeLib.t_gh_box,           new GH_BoxHint(),           "Param_Box_24x24" )},
            {typeof(Transform),     new MapItem (GH_TypeLib.t_gh_transform,     new GH_TransformHint(),     "Param_Transform_24x24" )},
            {typeof(Line),          new MapItem (GH_TypeLib.t_gh_line,          new GH_LineHint(),          "Param_Line_24x24" )},
            {typeof(Circle),        new MapItem (GH_TypeLib.t_gh_circle,        new GH_CircleHint(),        "Param_Circle_24x24" )},
            {typeof(Arc),           new MapItem (GH_TypeLib.t_gh_arc,           new GH_ArcHint(),           "Param_Arc_24x24")},
            {typeof(Polyline),      new MapItem (GH_TypeLib.t_gh_curve,         new GH_PolylineHint(),      "Param_Curve_24x24" )},
            {typeof(PolylineCurve), new MapItem (GH_TypeLib.t_gh_curve,         new GH_PolylineHint(),      "Param_Curve_24x24" )},
            {typeof(PolyCurve),     new MapItem (GH_TypeLib.t_gh_curve,         new GH_CurveHint(),         "Param_Curve_24x24" )},
            {typeof(Curve),         new MapItem (GH_TypeLib.t_gh_curve,         new GH_CurveHint(),         "Param_Curve_24x24" )},
            {typeof(Surface),       new MapItem (GH_TypeLib.t_gh_surface,       new GH_SurfaceHint(),       "Param_Surface_24x24" )},
            {typeof(Brep),          new MapItem (GH_TypeLib.t_gh_brep,          new GH_BrepHint(),          "Param_Brep_24x24" )},
            {typeof(Mesh),          new MapItem (GH_TypeLib.t_gh_mesh,          new GH_MeshHint(),          "Param_Mesh_24x24" )},
            {typeof(GeometryBase),  new MapItem (GH_TypeLib.t_gh_goo,           new GH_GeometryBaseHint(),  "Param_Geometry_24x24" )},
            {typeof(object),        new MapItem (GH_TypeLib.t_gh_objwrapper,    new GH_NullHint(),          "Param_Generic_24x24" )},
        };


        /*public static readonly Dictionary<Type, string> GhMap = new Dictionary<Type, string>()
        {
            //{typeof(UVInterval),    new RhMapRecord (GH_TypeLib.t_gh_uvinterval,    new GH_UVIntervalHint() )},
            //{ typeof (GH_Culture),      "Param_Culture_24x24" },
            //{ typeof (GH_Field),        "Param_Field_24x24" },
            //{ typeof (GH_Matrix),       "Param_Matrix_24x24" },
            //{ typeof (GH_MeshFace),     "Param_MeshFace_24x24" },
            //{ typeof (GH_Receiver),     "Param_Receiver_24x24" },
            //{ typeof (GH_2DInterval), "Param_2DInterval_24x24" },
            //{ typeof (GH_Cache), "Param_Cache_24x24" },
            //{ typeof (GH_Complex), "Param_Complex_24x24" },
            //{ typeof (GH_Constant), "Param_Constant_24x24" },
            //{ typeof (GH_DataPath), "Param_DataPath_24x24" },
            //{ typeof (GH_Generic) },
            //{ typeof (GH_Geometry) },
            //{ typeof (GH_GeometryPipeline), "Param_GeometryPipeline_24x24" },
            //{ typeof (GH_Group), "Param_Group_24x24" },
            //{ typeof (GH_Location), "Param_Location_24x24" },
            //{ typeof (GH_MeshSettings), "Param_MeshSettings_24x24" },
            //{ typeof (GH_OGLShader), "Param_OGLShader_24x24" },
            //{ typeof (GH_Path), "Param_Path_24x24" },
            //{ typeof (GH_ReadCache), "Param_ReadCache_24x24" },
            //{ typeof (GH_Script), "Param_Script_24x24" },
            //{ typeof (GH_WriteCache), "Param_WriteCache_24x24" }
        };*/
    }
}
