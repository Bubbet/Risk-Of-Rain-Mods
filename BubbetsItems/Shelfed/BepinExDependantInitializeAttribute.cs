using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HG.Reflection;
using UnityEngine;


namespace BubbetsItems
{
    /*
     * set up bepin dependencies for the plugin that calls execute based on the attributes defined types
     * if they are loaded by the time execute is ran then run the init method
     */
    public class BepinExDependantInitializeAttribute : SearchableAttribute // TODO replace searchable attribute with my own system that only scans this assembly
    {
        public Type[] dependencies = Array.Empty<Type>();
        private MethodInfo methodInfo;
        private Type associatedType;

        public BepinExDependantInitializeAttribute(params Type[] dependencies)
        {
            if (dependencies != null)
            {
                this.dependencies = dependencies;
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
                    bepinDepInitAttribute.methodInfo = methodInfo;
                    bepinDepInitAttribute.associatedType = methodInfo.DeclaringType;
                }
            }

            var initializedTypes = new HashSet<Type>();
            var initializationLogHandler = new BepinExDependantInitializeLogHandler();
            //var logHandler = Assembly.GetCallingAssembly(). TODO get the logger from the bepinchainloader and this function, figure out what plugin called this function;
            var logHandler = Debug.unityLogger.logHandler;
            initializationLogHandler.underlyingLogHandler = logHandler;

            var num = 0;
            while (queue.Count > 0)
            {
                var attribute = queue.Dequeue();
                if (!InitializerDependenciesMet(attribute))
                {
                    queue.Enqueue(attribute);
                    ++num;
                    if (num >= queue.Count)
                    {
                        Debug.LogFormat(
                            nameof(BepinExDependantInitializeAttribute) + " infinite loop detected. currentMethod={0}",
                            attribute.associatedType.FullName + attribute.methodInfo.Name);
                        break;
                    }
                }
                else
                {
                    try
                    {
                        Debug.unityLogger.logHandler = initializationLogHandler;
                        initializationLogHandler.currentInitializer = attribute;
                        attribute.methodInfo.Invoke(null, new object[] { });
                        initializedTypes.Add(attribute.associatedType);
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
        }

        public static bool InitializerDependenciesMet(BepinExDependantInitializeAttribute attribute)
        {
            return attribute.dependencies.All(dependency => Chainloader.PluginInfos.Any(x => x.Value.Instance.GetType() == dependency));
        }

        private class BepinExDependantInitializeLogHandler : ILogHandler
        {
            public ILogHandler underlyingLogHandler;
            private BepinExDependantInitializeAttribute _currentInitializer;
            private string logPrefix = string.Empty;

            public BepinExDependantInitializeAttribute currentInitializer
            {
                get => _currentInitializer;
                set
                {
                    _currentInitializer = value;
                    logPrefix = "[" + currentInitializer.associatedType.FullName + "] ";
                }
            }

            public void LogException(Exception exception, UnityEngine.Object context) => LogFormat(LogType.Exception, context, exception.Message, null);

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args) => underlyingLogHandler.LogFormat(logType, context, logPrefix + format, args);
        }
    }
}