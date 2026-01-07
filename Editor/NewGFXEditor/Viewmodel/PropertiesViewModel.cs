using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Viewmodel
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class PropertiesViewModel
    {
        private Dictionary<string, string> _properties;
        private Action<Dictionary<string, string>> _onChanged;

        public Dictionary<string, string> Properties => _properties;

        public PropertiesViewModel(Action<Dictionary<string, string>> onChanged)
        {
            _properties = new Dictionary<string, string>();
            _onChanged = onChanged;
        }

        public void Refresh(Dictionary<string, object> properties)
        {
            _properties.Clear();
            foreach(var kvp in properties)
            {
                _properties[kvp.Key] = kvp.Value?.ToString() ?? "<null>";
            }
        }

        public void Clear()
        {
            _properties.Clear();
        }

        public override string ToString() => "Properties";

    }
}
