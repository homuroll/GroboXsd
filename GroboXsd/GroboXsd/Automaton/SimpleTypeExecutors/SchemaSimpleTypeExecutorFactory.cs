using System.Collections;

using GroboXsd.Parser;

using JetBrains.Annotations;

namespace GroboXsd.Automaton.SimpleTypeExecutors
{
    public class SchemaSimpleTypeExecutorFactory : ISchemaSimpleTypeExecutorFactory
    {
        public SchemaSimpleTypeExecutorFactory()
        {
            executors.Add(SchemaSimpleType.String, new StringSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.Integer, new IntegerSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.Int, new IntSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.Boolean, new BooleanSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.Decimal, new DecimalSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.GYear, new GYearSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.GMonth, new GMonthSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.Date, new DateSimpleTypeExecutor());
            executors.Add(SchemaSimpleType.AnyURI, new AnyURISimpleTypeExecutor());
            executors.Add(SchemaSimpleType.Base64Binary, new Base64BinarySimpleTypeExecutor());
        }

        [NotNull]
        public ISchemaSimpleTypeExecutor Build([NotNull] SchemaSimpleType schemaSimpleType)
        {
            var executor = (ISchemaSimpleTypeExecutor)executors[schemaSimpleType];
            if(executor == null)
            {
                lock(executorsLock)
                {
                    executor = (ISchemaSimpleTypeExecutor)executors[schemaSimpleType];
                    if(executor == null)
                        executors[schemaSimpleType] = executor = BuildNoLock(schemaSimpleType);
                }
            }
            return executor;
        }

        [NotNull]
        private ISchemaSimpleTypeExecutor BuildNoLock([NotNull] SchemaSimpleType schemaSimpleType)
        {
            var baseExecutor = schemaSimpleType.BaseType == null ? null : Build((SchemaSimpleType)schemaSimpleType.BaseType);
            return new SchemaSimpleTypeExecutor(baseExecutor, schemaSimpleType);
        }

        private readonly Hashtable executors = new Hashtable();
        private readonly object executorsLock = new object();
    }
}