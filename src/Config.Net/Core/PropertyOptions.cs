﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Config.Net.Core
{
   class PropertyOptions
   {
      public PropertyOptions(string name, Type type, object defaultValue)
      {
         Name = name;

         Type = type;

         TypeInfo ti = type.GetTypeInfo();
         if(ti.IsClass)
         {
            BaseType = type;
         }
         else
         {
            if(ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
               BaseType = ti.GenericTypeArguments[0];
            }
            else
            {
               BaseType = type;
            }
         }

         DefaultValue = defaultValue;
      }

      public string Name { get; }

      public Type Type { get; }

      public Type BaseType { get; }

      public object DefaultValue { get; }

      public static Dictionary<string, PropertyOptions> Discover<TInterface>()
      {
         var result = new Dictionary<string, PropertyOptions>();

         IEnumerable<PropertyInfo> properties = GetHierarchyPublicProperties(typeof(TInterface));

         foreach(PropertyInfo pi in properties)
         {
            OptionAttribute attribute = pi.GetCustomAttribute<OptionAttribute>();

            string name = pi.Name;
            object defaultValue = null;

            if(attribute != null)
            {
               if (attribute.Alias != null) name = attribute.Alias;

               //validate that types for default value match
               Type dvt = attribute.DefaultValue?.GetType();
               if(attribute.DefaultValue != null)
               {
                  if (dvt != pi.PropertyType && dvt != typeof(string))
                  {
                     throw new InvalidCastException($"Default value for option {name} is of type {dvt.FullName} whereas the property has type {pi.PropertyType.FullName}. To fix this, either set default value to type {pi.PropertyType.FullName} or a string parseable to the target type.");
                  }

                  if(pi.PropertyType != typeof(string) && dvt == typeof(string))
                  {
                     ValueHandler.Default.TryParse(pi.PropertyType, (string)attribute.DefaultValue, out defaultValue);
                  }
               }

               if (defaultValue == null)
               {
                  defaultValue = attribute.DefaultValue;
               }
            }

            if (defaultValue == null) defaultValue = GetDefaultValue(pi.PropertyType);

            result[pi.Name] = new PropertyOptions(name, pi.PropertyType, defaultValue);
         }

         return result;
      }

      private static object GetDefaultValue(Type t)
      {
         if (t.GetTypeInfo().IsValueType) return Activator.CreateInstance(t);

         return null;
      }

      private static PropertyInfo[] GetHierarchyPublicProperties(Type type)
      {
         var propertyInfos = new List<PropertyInfo>();

         var considered = new List<TypeInfo>();
         var queue = new Queue<TypeInfo>();
         considered.Add(type.GetTypeInfo());
         queue.Enqueue(type.GetTypeInfo());

         while (queue.Count > 0)
         {
            TypeInfo typeInfo = queue.Dequeue();

            //add base interfaces to the queue
            foreach (Type subInterface in typeInfo.ImplementedInterfaces)
            {
               TypeInfo subInterfaceTypeInfo = subInterface.GetTypeInfo();

               if (considered.Contains(subInterfaceTypeInfo)) continue;

               considered.Add(subInterfaceTypeInfo);
               queue.Enqueue(subInterfaceTypeInfo);
            }

            //add base classes to the queue
            if (typeInfo.BaseType != null)
            {
               TypeInfo baseType = typeInfo.BaseType.GetTypeInfo();

               if (!considered.Contains(baseType))
               {
                  considered.Add(baseType);
                  queue.Enqueue(baseType);
               }
            }


            //get properties from the current type
            IEnumerable<PropertyInfo> newProperties = typeInfo.DeclaredProperties.Where(p => !propertyInfos.Contains(p));
            propertyInfos.InsertRange(0, newProperties);
         }

         return propertyInfos.ToArray();
      }
   }
}
