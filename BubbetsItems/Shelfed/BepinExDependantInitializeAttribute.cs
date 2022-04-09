using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HG.Reflection;
using UnityEngine;

namespace BubbetsItems.Shelfed
{
    /*
     * set up bepin dependencies for the plugin that calls execute based on the attributes defined types
     * if they are loaded by the time execute is ran then run the init method
     */
    public class BepinExDependantInitializeAttribute : SearchableAttribute // TODO replace searchable attribute with my own system that only scans this assembly
    {
        public readonly Type[] Dependencies = Array.Empty<Type>();
        private MethodInfo? _methodInfo;
        private Type? _associatedType;

        public BepinExDependantInitializeAttribute(params Type[]? dependencies)
        {
            if (dependencies != null)
            {
                this.Dependencies = dependencies;
            }
        }

        public static void Execute()
        {
            var queue = new Queue<BepinExDependantInitializeAttribute>();
            foreach (var searchableAttribute in GetInstances<BepinExDependantInitializeAttribute>())
            {
                var bepinDepInitAttribute = (BepinExDependantInitializeAttribute) searchableAttribute;
                var methodInfo = bepinDepInitAttribute.target as MethodInfo;
                if (methodInfo != null && methodInfo.IsStatic)
                {
                    queue.Enqueue(bepinDepInitAttribute);
                    bepinDepInitAttribute._methodInfo = methodInfo;
                    bepinDepInitAttribute._associatedType = methodInfo.DeclaringType;
                }
            }

            var initializedTypes = new HashSet<Type>();
            var initializationLogHandler = new BepinExDependantInitializeLogHandler();
            //var logHandler = Assembly.GetCallingAssembly(). TODO get the logger from the bepinchainloader and this function, figure out what plugin called this function;
            var logHandler = Debug.unityLogger.logHandler;
            initializationLogHandler.UnderlyingLogHandler = logHandler;

            var num = 0;
            while (queue.Count > 0)
            {
                var attribute = queue.Dequeue();
                if (!InitializerDependenciesMet(attribute))
                {
                    queue.Enqueue(attribute);
                    ++num;
                    if (num < queue.Count) continue;
                    if (attribute._associatedType is not null && attribute._methodInfo is not null)
                        Debug.LogFormat(nameof(BepinExDependantInitializeAttribute) + " infinite loop detected. currentMethod={0}", attribute._associatedType.FullName + attribute._methodInfo.Name);
                    break;
                }

                try
                {
                    Debug.unityLogger.logHandler = initializationLogHandler;
                    initializationLogHandler.CurrentInitializer = attribute;
                    if (attribute._methodInfo is null || attribute._associatedType is null)
                    {
                        num = 0;
                        continue;
                    }
                    attribute._methodInfo.Invoke(null, new object[] { });
                    initializedTypes.Add(attribute._associatedType);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                finally
                {
                    Debug.unityLogger.logHandler = logHandler;
                }
                num = 0;
            }
        }

        private static bool InitializerDependenciesMet(BepinExDependantInitializeAttribute attribute)
        {
            return attribute.Dependencies.All(dependency => Chainloader.PluginInfos.Any(x => x.Value.Instance.GetType() == dependency));
        }

        private class BepinExDependantInitializeLogHandler : ILogHandler
        {
            public ILogHandler? UnderlyingLogHandler;
            private BepinExDependantInitializeAttribute? _currentInitializer;
            private string _logPrefix = string.Empty;

            public BepinExDependantInitializeAttribute CurrentInitializer
            {
                get => _currentInitializer ?? throw new InvalidOperationException();
                set
                {
                    _currentInitializer = value;
                    _logPrefix = "[" + CurrentInitializer._associatedType!.FullName + "] ";
                }
            }

            public void LogException(Exception exception, UnityEngine.Object context) => LogFormat(LogType.Exception, context, exception.Message);

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args) => UnderlyingLogHandler?.LogFormat(logType, context, _logPrefix + format, args);
        }
    }
}