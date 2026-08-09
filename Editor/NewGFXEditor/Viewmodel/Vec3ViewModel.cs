using FreeTypeSharp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Viewmodel
{
    public class FloatConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str)
            {
                if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
                if (float.TryParse(str, NumberStyles.Float, culture, out result))
                {
                    return result;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is float floatValue)
            {
                return floatValue.ToString(CultureInfo.InvariantCulture);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Vec3ViewModel
    {
        private Vector3 _value;
        private readonly Action<Vector3> _changed;

        public Vec3ViewModel(Vector3 value, Action<Vector3> onChanged)
        {
            _value = value;
            _changed = onChanged;
        }

        [TypeConverter(typeof(FloatConverter))]
        public float X
        {
            get => _value.X;
            set => UpdateX(value);
        }

        [TypeConverter(typeof(FloatConverter))]
        public float Y
        {
            get => _value.Y;
            set => UpdateY(value);
        }

        [TypeConverter(typeof(FloatConverter))]
        public float Z
        {
            get => _value.Z;
            set => UpdateZ(value);
        }

        public void UpdateX(float value)
        {
            _value = new Vector3(value, _value.Y, _value.Z);
            _changed(_value);
        }

        public void UpdateY(float value)
        {
            _value = new Vector3(_value.X, value, _value.Z);
            _changed(_value);
        }

        public void UpdateZ(float value)
        {
            _value = new Vector3(_value.X, _value.Y, value);
            _changed(_value);
        }

        public void Refresh(Vector3 value)
        {
            _value = value;
        }

        public override string ToString() => "Vector3";
    }
}
