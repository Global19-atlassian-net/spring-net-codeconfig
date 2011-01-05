﻿#region License

/*
 * Copyright © 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using Common.Logging;
using Spring.Objects.Factory.Parsing;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Config;
using Spring.Collections.Generic;
using Spring.Objects.Factory;
using Spring.Core;
using Spring.Aop.Framework;
using Spring.Context.Advice;
using Spring.Aop.Framework.DynamicProxy;
using Spring.Aop;

namespace Spring.Context.Attributes
{
    public class ConfigurationClassPostProcessor : IObjectDefinitionRegistryPostProcessor, IOrdered
    {
        private ILog _logger = LogManager.GetLogger(typeof(ConfigurationClassPostProcessor));

        private bool _postProcessObjectDefinitionRegistryCalled = false;

        private bool _postProcessObjectFactoryCalled = false;

        private IProblemReporter _problemReporter = new FailFastProblemReporter();

        public int Order
        {
            get { return int.MinValue; }
        }

        public IProblemReporter ProblemReporter
        {
            set { _problemReporter = (value ?? new FailFastProblemReporter()); }
        }

        public void PostProcessObjectDefinitionRegistry(IObjectDefinitionRegistry registry)
        {
            if (_postProcessObjectDefinitionRegistryCalled)
            {
                throw new InvalidOperationException("PostProcessObjectDefinitionRegistry already called for this post-processor");
            }
            if (_postProcessObjectFactoryCalled)
            {
                throw new InvalidOperationException("PostProcessObjectFactory already called for this post-processor");
            }
            _postProcessObjectDefinitionRegistryCalled = true;
            ProcessConfigObjectDefinitions(registry);
        }

        public void PostProcessObjectFactory(IConfigurableListableObjectFactory objectFactory)
        {
            if (_postProcessObjectFactoryCalled)
            {
                throw new InvalidOperationException(
                        "PostProcessObjectFactory already called for this post-processor");
            }
            _postProcessObjectFactoryCalled = true;
            if (!_postProcessObjectDefinitionRegistryCalled)
            {
                // ObjectDefinitionRegistryPostProcessor hook apparently not supported...
                // Simply call processConfigObjectDefinitions lazily at this point then.
                ProcessConfigObjectDefinitions((IObjectDefinitionRegistry)objectFactory);
            }

            EnhanceConfigurationClasses(objectFactory);
        }

        private void EnhanceConfigurationClasses(IConfigurableListableObjectFactory objectFactory)
        {
            string[] objectNames = objectFactory.GetObjectDefinitionNames();

            foreach (string t in objectNames)
            {
                IObjectDefinition objDef = objectFactory.GetObjectDefinition(t);

                if (((AbstractObjectDefinition)objDef).HasObjectType)
                {
                    if (Attribute.GetCustomAttribute(objDef.ObjectType, typeof(ConfigurationAttribute)) != null)
                    {

                        ProxyFactory proxyFactory = new ProxyFactory();
                        proxyFactory.ProxyTargetAttributes = true;
                        proxyFactory.Interfaces = Type.EmptyTypes;
                        proxyFactory.TargetSource = new ObjectFactoryTargetSource(t, objectFactory);
                        SpringObjectMethodInterceptor methodInterceptor = new SpringObjectMethodInterceptor(objectFactory);
                        proxyFactory.AddAdvice(methodInterceptor);

                        //TODO check type of object isn't infrastructure type.

                        InheritanceAopProxyTypeBuilder iaptb = new InheritanceAopProxyTypeBuilder(proxyFactory);
                        //iaptb.ProxyDeclaredMembersOnly = true; // make configurable.
                        ((IConfigurableObjectDefinition)objDef).ObjectType = iaptb.BuildProxyType();

                        objDef.ConstructorArgumentValues.AddIndexedArgumentValue(objDef.ConstructorArgumentValues.ArgumentCount, proxyFactory);

                    }
                }
            }
        }

        private Type GenerateProxyType(string objectName, IConfigurableListableObjectFactory objectFactory)
        {
            ProxyFactory proxyFactory = new ProxyFactory();
            proxyFactory.ProxyTargetAttributes = true;
            proxyFactory.Interfaces = Type.EmptyTypes;
            proxyFactory.TargetSource = new ObjectFactoryTargetSource(objectName, objectFactory);
            SpringObjectMethodInterceptor methodInterceptor = new SpringObjectMethodInterceptor(objectFactory);
            proxyFactory.AddAdvice(methodInterceptor);

            //TODO check type of object isn't infrastructure type.

            InheritanceAopProxyTypeBuilder iaptb = new InheritanceAopProxyTypeBuilder(proxyFactory);
            //iaptb.ProxyDeclaredMembersOnly = true; // make configurable.
            return iaptb.BuildProxyType();
        }

        private void ProcessConfigObjectDefinitions(IObjectDefinitionRegistry registry)
        {
            ISet<ObjectDefinitionHolder> configCandidates = new HashedSet<ObjectDefinitionHolder>();
            foreach (string objectName in registry.GetObjectDefinitionNames())
            {
                IObjectDefinition objectDef = registry.GetObjectDefinition(objectName);
                if (ConfigurationClassObjectDefinitionReader.CheckConfigurationClassCandidate(objectDef))
                {
                    configCandidates.Add(new ObjectDefinitionHolder(objectDef, objectName));
                }
            }

            //if nothing to process, bail out
            if (configCandidates.Count == 0) { return; }

            ConfigurationClassParser parser = new ConfigurationClassParser(_problemReporter);
            foreach (ObjectDefinitionHolder holder in configCandidates)
            {
                IObjectDefinition bd = holder.ObjectDefinition;
                try
                {
                    if (bd is AbstractObjectDefinition && ((AbstractObjectDefinition)bd).HasObjectType)
                    {
                        parser.Parse(((AbstractObjectDefinition)bd).ObjectType, holder.ObjectName);
                    }
                    else
                    {
                        //parser.Parse(bd.ObjectTypeName, holder.ObjectName);
                    }
                }
                catch (ObjectDefinitionParsingException ex)
                {
                    throw new ObjectDefinitionStoreException("Failed to load object class: " + bd.ObjectTypeName, ex);
                }
            }
            parser.Validate();

            // Read the model and create Object definitions based on its content
            ConfigurationClassObjectDefinitionReader reader = new ConfigurationClassObjectDefinitionReader(registry, _problemReporter);
            reader.LoadObjectDefinitions(parser.ConfigurationClasses);
        }

        public class ObjectFactoryTargetSource : ITargetSource
        {
            private readonly string _objectName;
            private readonly IConfigurableListableObjectFactory _objectFactory;

            public ObjectFactoryTargetSource(string objectName, IConfigurableListableObjectFactory objectFactory)
            {
                _objectName = objectName;
                _objectFactory = objectFactory;
            }

            #region ITargetSource Members

            public object GetTarget()
            {
                return _objectFactory.GetObject(_objectName);
            }

            public bool IsStatic
            {
                get { return _objectFactory.IsSingleton(_objectName); }
            }

            public void ReleaseTarget(object target)
            {
            }

            public Type TargetType
            {
                get { return _objectFactory.GetType(_objectName); }
            }

            #endregion
        }
    }
}
