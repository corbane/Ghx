using System;

namespace Ghx.RoslynScript
{
    public interface ICsxAttribute
    {
        string NickName { get; }
        string Description { get; }
        object DefaultValue { get; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class Input : Attribute, ICsxAttribute
    {
        public string NickName { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }

        //fromGH
         
        public Input(string nickname, string description = "", object defaultValue = null)
        {
            NickName = nickname;
            Description = description;
            DefaultValue = defaultValue;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class Output : Attribute, ICsxAttribute
    {
        public string NickName { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }

        //toGH

        public Output(string nickname, string description = "", object defaultValue = null)
        {
            NickName = nickname;
            Description = description;
            DefaultValue = defaultValue;
        }
    }
}
