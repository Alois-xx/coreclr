// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal unsafe sealed class RuntimeEventInfo : EventInfo
    {
        #region Private Data Members
        private int m_token;
        private EventAttributes m_flags;
        private string m_name;
        private void* m_utf8name;
        private RuntimeTypeCache m_reflectedTypeCache;
        private RuntimeMethodInfo m_addMethod;
        private RuntimeMethodInfo m_removeMethod;
        private RuntimeMethodInfo m_raiseMethod;
        private MethodInfo[] m_otherMethod;
        private RuntimeType m_declaringType;
        private BindingFlags m_bindingFlags;
        #endregion

        #region Constructor
        internal RuntimeEventInfo()
        {
            // Used for dummy head node during population
        }
        internal RuntimeEventInfo(int tkEvent, RuntimeType declaredType, RuntimeTypeCache reflectedTypeCache, out bool isPrivate)
        {
            Contract.Requires(declaredType != null);
            Contract.Requires(reflectedTypeCache != null);
            Debug.Assert(!reflectedTypeCache.IsGlobal);

            MetadataImport scope = declaredType.GetRuntimeModule().MetadataImport;

            m_token = tkEvent;
            m_reflectedTypeCache = reflectedTypeCache;
            m_declaringType = declaredType;


            RuntimeType reflectedType = reflectedTypeCache.GetRuntimeType();

            scope.GetEventProps(tkEvent, out m_utf8name, out m_flags);

            RuntimeMethodInfo dummy;
            Associates.AssignAssociates(scope, tkEvent, declaredType, reflectedType,
                out m_addMethod, out m_removeMethod, out m_raiseMethod,
                out dummy, out dummy, out m_otherMethod, out isPrivate, out m_bindingFlags);
        }
        #endregion

        #region Internal Members
        internal override bool CacheEquals(object o)
        {
            RuntimeEventInfo m = o as RuntimeEventInfo;

            if ((object)m == null)
                return false;

            return m.m_token == m_token &&
                RuntimeTypeHandle.GetModule(m_declaringType).Equals(
                    RuntimeTypeHandle.GetModule(m.m_declaringType));
        }

        internal BindingFlags BindingFlags { get { return m_bindingFlags; } }
        #endregion

        #region Object Overrides
        public override String ToString()
        {
            if (m_addMethod == null || m_addMethod.GetParametersNoCopy().Length == 0)
                throw new InvalidOperationException(SR.InvalidOperation_NoPublicAddMethod);

            return m_addMethod.GetParametersNoCopy()[0].ParameterType.FormatTypeName() + " " + Name;
        }
        #endregion

        #region ICustomAttributeProvider
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return MemberTypes.Event; } }
        public override String Name
        {
            get
            {
                if (m_name == null)
                    m_name = new Utf8String(m_utf8name).ToString();

                return m_name;
            }
        }
        public override Type DeclaringType { get { return m_declaringType; } }
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeEventInfo>(other);
        public override Type ReflectedType
        {
            get
            {
                return ReflectedTypeInternal;
            }
        }

        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        public override int MetadataToken { get { return m_token; } }
        public override Module Module { get { return GetRuntimeModule(); } }
        internal RuntimeModule GetRuntimeModule() { return m_declaringType.GetRuntimeModule(); }
        #endregion

        #region EventInfo Overrides
        public override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            List<MethodInfo> ret = new List<MethodInfo>();

            if ((object)m_otherMethod == null)
                return new MethodInfo[0];

            for (int i = 0; i < m_otherMethod.Length; i++)
            {
                if (Associates.IncludeAccessor((MethodInfo)m_otherMethod[i], nonPublic))
                    ret.Add(m_otherMethod[i]);
            }

            return ret.ToArray();
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_addMethod, nonPublic))
                return null;

            return m_addMethod;
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_removeMethod, nonPublic))
                return null;

            return m_removeMethod;
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_raiseMethod, nonPublic))
                return null;

            return m_raiseMethod;
        }

        public override EventAttributes Attributes
        {
            get
            {
                return m_flags;
            }
        }
        #endregion    
    }
}
