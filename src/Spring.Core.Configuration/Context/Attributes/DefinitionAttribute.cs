﻿using System;
using System.Collections.Generic;
using System.Text;
using Spring.Objects.Factory.Config;
using Spring.Util;

namespace Spring.Context.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DefinitionAttribute : Attribute
    {
        private AutoWiringMode _autoWire = AutoWiringMode.No;

        private string _destroyMethod;

        private string _initMethod;

        private string _names;

        /// <summary>
        /// Are dependencies to be injected via autowiring?
        /// </summary>
        /// <value>The auto wire.</value>
        public AutoWiringMode AutoWire
        {
            get { return _autoWire; }
            set
            {
                _autoWire = value;
            }
        }

        /// <summary>
        /// The optional name of a method to call on the Object instance upon closing the
        /// application context, for example a Close() method on a DataSource.
        /// The method must have no arguments but may throw any exception.
        /// <para>
        /// Note: Only invoked on objects whose lifecycle is under the full control of the
        /// factory, which is always the case for singletons but not guaranteed 
        /// for any other scope.
        /// </para>
        /// <see cref="Spring.Context.IConfigurableApplicationContext"/>
        /// </summary>
        /// <value>The destroy method.</value>
        public string DestroyMethod
        {
            get { return _destroyMethod; }
            set
            {
                _destroyMethod = value;
            }
        }

        /// <summary>
        /// The optional name of a method to call on the object instance during initialization.
        /// Not commonly used, given that the method may be called programmatically directly
        /// within the body of a Object-annotated method.
        /// </summary>
        /// <value>The init method.</value>
        public string InitMethod
        {
            get { return _initMethod; }
            set
            {
                _initMethod = value;
            }
        }

        /// <summary>
        /// The name of this object, or if plural, aliases for this object. If left unspecified
        /// the name of the object is the name of the attributed method. If specified, the method
        /// name is ignored.
        /// </summary>
        /// <value>The name.</value>
        public string Names
        {
            get
            {
                return _names;
            }
            set
            {
                _names = value;
            }
        }

        public string[] NamesToArray
        {
            get
            {
                return StringUtils.DelimitedListToStringArray(_names, ",");
            }
        }

    }
}
