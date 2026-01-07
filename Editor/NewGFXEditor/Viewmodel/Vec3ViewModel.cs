using FreeTypeSharp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGFXEditor.Viewmodel
{
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

        public float X
        {
            get => _value.X;
            set => UpdateX(value);
        }

        public float Y
        {
            get => _value.Y;
            set => UpdateY(value);
        }

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
