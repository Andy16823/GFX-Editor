using LibGFX.Core;
using OpenTK.Graphics.GL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assimp.Metadata;

namespace NewGFXEditor.Viewmodel
{
    /// <summary>
    /// Element view model for editing GameElement properties in the UI.
    /// </summary>
    public class ElementViewModel
    {
        private GameElement _element;
        private Vec3ViewModel _position;
        private Vec3ViewModel _rotation;
        private Vec3ViewModel _scale;

        [Category("General")]
        public string Name { 
            get => _element?.Name ?? "<none>"; 
            set
            {
                if (_element != null)
                    _element.Name = value;
            }
        }

        [Category("General")]
        public bool Visible
        {
            get => _element?.Visible ?? false;
            set
            {
                if (_element != null)
                    _element.Visible = value;
            }
        }

        [Category("Transform")]
        public Vec3ViewModel Position => _position;

        [Category("Transform")]
        public Vec3ViewModel Rotation => _rotation;

        [Category("Transform")]
        public Vec3ViewModel Scale => _scale;

        public event Action<ElementViewModel> Refreshed;
        public event Action<ElementViewModel> Released;

        public ElementViewModel()
        {
            _position = new Vec3ViewModel(Vector3.Zero, v =>
            {
                if (_element != null)
                    _element.Transform.Position = v;
            });

            _rotation = new Vec3ViewModel(Vector3.Zero, v =>
            {
                if (_element != null)
                    _element.Transform.SetRotation(v);
            });

            _scale = new Vec3ViewModel(Vector3.One, v =>
            {
                if (_element != null)
                    _element.Transform.Scale = v;
            });
        }

        public void SetElement(GameElement? element)
        {
            _element = element;
            if (_element != null)
            {
                _position.Refresh(_element.Transform.Position);
                _rotation.Refresh(_element.Transform.GetEulerAngles());
                _scale.Refresh(_element.Transform.Scale);
            }
            else
            {
                _position.Refresh(Vector3.Zero);
                _rotation.Refresh(Vector3.Zero);
                _scale.Refresh(Vector3.One);
            }
        }

        public void ReleaseElement()
        {
            _element = null;
            _position.Refresh(Vector3.Zero);
            _rotation.Refresh(Vector3.Zero);
            _scale.Refresh(Vector3.One);
            Released?.Invoke(this);
        }

        public void Refresh()
        {
            if (_element != null)
            {
                _position.Refresh(_element.Transform.Position);
                _rotation.Refresh(_element.Transform.GetEulerAngles());
                _scale.Refresh(_element.Transform.Scale);
                Refreshed?.Invoke(this);
            }
        }
    }
}
