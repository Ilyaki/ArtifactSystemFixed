using System.Reflection;
using System.Linq;
using Harmony;
using System;

namespace ArtifactSystemFixed
{
    internal abstract class Patch
    {
        protected abstract PatchDescriptor GetPatchDescriptor();

        private void ApplyPatch(HarmonyInstance harmonyInstance)
        {
            var patchDescriptor = GetPatchDescriptor();

            var targetMethod = string.IsNullOrEmpty(patchDescriptor.targetMethodName)
                             ? (MethodBase) patchDescriptor.targetType.GetConstructor(patchDescriptor.targetMethodArguments)
                             : patchDescriptor.targetMethodArguments != null
                                 ? patchDescriptor.targetType.GetMethod(patchDescriptor.targetMethodName, patchDescriptor.targetMethodArguments)
                                 : patchDescriptor.targetType.GetMethod(patchDescriptor.targetMethodName);

            harmonyInstance.Patch(targetMethod, new HarmonyMethod(GetType().GetMethod("Prefix")),
                                                new HarmonyMethod(GetType().GetMethod("Postfix")));
        }

        public static void PatchAll(HarmonyInstance harmonyInstance)
        {
            var patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && type.BaseType == typeof(Patch));

            foreach (var patch in patches)
                ((Patch) Activator.CreateInstance(patch)).ApplyPatch(harmonyInstance);
        }

        protected class PatchDescriptor
        {
            public Type[] targetMethodArguments;
            public string targetMethodName;
            public Type targetType;

            /// <param name="targetType">Don't use typeof() or it won't work on other platforms</param>
            /// <param name="targetMethodName">Null if constructor is desired</param>
            /// <param name="targetMethodArguments">Null if no method ambiguity</param>
            public PatchDescriptor(Type targetType, string targetMethodName, Type[] targetMethodArguments = null)
            {
                this.targetType = targetType;
                this.targetMethodName = targetMethodName;
                this.targetMethodArguments = targetMethodArguments;
            }
        }
    }
}