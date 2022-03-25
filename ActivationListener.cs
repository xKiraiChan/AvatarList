using System;
using UnhollowerBaseLib.Attributes;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace AvatarList
{
    public class ActivationListener : MonoBehaviour
    {
        static ActivationListener() => ClassInjector.RegisterTypeInIl2Cpp<ActivationListener>();

        [HideFromIl2Cpp]
        public event Action OnActivate;
        
        public ActivationListener() : base(ClassInjector.DerivedConstructorPointer<ActivationListener>()) => ClassInjector.DerivedConstructorBody(this);
        public ActivationListener(IntPtr ptr) : base(ptr) { }

        public void OnEnable() => OnActivate?.Invoke();
    }
}
