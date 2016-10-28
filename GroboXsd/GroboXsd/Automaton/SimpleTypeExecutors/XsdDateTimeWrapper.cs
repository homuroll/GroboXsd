using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;

using GrEmit;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class XsdDateTimeWrapper
    {
        private XsdDateTimeWrapper(object xsdDateTime)
        {
            this.xsdDateTime = xsdDateTime;
        }

        [CanBeNull]
        public static XsdDateTimeWrapper Parse([NotNull] string value, DateTimeTypeCode typeCode)
        {
            XsdDateTimeWrapper result;
            return TryParse(value, typeCode, out result) ? null : result;
        }

        public static bool TryParse([NotNull] string value, DateTimeTypeCode typeCode, out XsdDateTimeWrapper result)
        {
            object xsdDateTime;
            if(tryParseDelegate(value, typeCode, out xsdDateTime))
            {
                result = new XsdDateTimeWrapper(xsdDateTime);
                return true;
            }
            result = null;
            return false;
        }

        public int CompareTo(XsdDateTimeWrapper other)
        {
            return compareDelegate(xsdDateTime, other.xsdDateTime);
        }

        private static TryParseDelegate EmitTryParseDelegate()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(bool),
                                           new[] {typeof(string), typeof(DateTimeTypeCode), typeof(object).MakeByRefType()}, typeof(string), true);
            var xsdDateTimeType = typeof(XmlReader).Assembly.GetTypes().FirstOrDefault(type => type.Name == "XsdDateTime");
            if(xsdDateTimeType == null)
                throw new InvalidOperationException("The type 'XsdDateTime' is not found");
            var tryParseMethod = xsdDateTimeType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.NonPublic);
            if(tryParseMethod == null)
                throw new InvalidOperationException("The method 'XsdDateTime.TryParse' is not found");
            using(var il = new GroboIL(method))
            {
                il.Ldarg(0); // stack: [value]
                il.Ldarg(1); // stack: [value, typeCode]
                var strongBoxType = typeof(StrongBox<>).MakeGenericType(xsdDateTimeType);
                var strongBox = il.DeclareLocal(strongBoxType);
                il.Ldarg(2); // stack: [value, typeCode, ref result]
                il.Newobj(strongBoxType.GetConstructor(Type.EmptyTypes)); // stack: [value, typeCode, ref result, new StrongBox<XsdDateTime>()]
                il.Dup();
                il.Stloc(strongBox); // strongBox = new StrongBox<XsdDateTime>(); stack: [value, typeCode, ref result, strongBox]
                il.Stind(typeof(object)); // result = strongBox; stack: [value, typeCode]
                il.Ldloc(strongBox); // stack: [value, typeCode, strongBox]
                il.Ldflda(strongBoxType.GetField("Value", BindingFlags.Instance | BindingFlags.Public)); // stack: [value, typeCode, ref strongBox.Value]
                il.Call(tryParseMethod); // stack: [XsdDateTime.TryParse(value, typeCode, ref strongBox.Value]
                il.Ret();
            }
            return (TryParseDelegate)method.CreateDelegate(typeof(TryParseDelegate));
        }

        private static Func<object, object, int> EmitCompareDelegate()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(object), typeof(object)}, typeof(string), true);
            var xsdDateTimeType = typeof(XmlReader).Assembly.GetTypes().FirstOrDefault(type => type.Name == "XsdDateTime");
            if(xsdDateTimeType == null)
                throw new InvalidOperationException("The type 'XsdDateTime' is not found");
            var compareMethod = xsdDateTimeType.GetMethod("Compare", BindingFlags.Static | BindingFlags.Public);
            if(compareMethod == null)
                throw new InvalidOperationException("The method 'XsdDateTime.Compare' is not found");
            using(var il = new GroboIL(method))
            {
                var strongBoxType = typeof(StrongBox<>).MakeGenericType(xsdDateTimeType);
                var valueField = strongBoxType.GetField("Value", BindingFlags.Instance | BindingFlags.Public);
                il.Ldarg(0); // stack: [arg0]
                il.Castclass(strongBoxType); // stack: [(StrongBox<XsdDateTime>)arg0]
                il.Ldfld(valueField); // stack: [((StrongBox<XsdDateTime>)arg0).Value]
                il.Ldarg(1); // stack: [((StrongBox<XsdDateTime>)arg0).Value, arg1]
                il.Castclass(strongBoxType); // stack: [((StrongBox<XsdDateTime>)arg0).Value, (StrongBox<XsdDateTime>)arg1]
                il.Ldfld(valueField); // stack: [((StrongBox<XsdDateTime>)arg0).Value, ((StrongBox<XsdDateTime>)arg1).Value]
                il.Call(compareMethod); // stack: [XsdDateTime.CompareTo(((StrongBox<XsdDateTime>)arg0).Value, ((StrongBox<XsdDateTime>)arg1).Value)]
                il.Ret();
            }
            return (Func<object, object, int>)method.CreateDelegate(typeof(Func<object, object, int>));
        }

        private delegate bool TryParseDelegate(string value, DateTimeTypeCode typeCode, out object result);

        private static readonly Func<object, object, int> compareDelegate = EmitCompareDelegate();

        private static readonly TryParseDelegate tryParseDelegate = EmitTryParseDelegate();
        private readonly object xsdDateTime;
    }
}