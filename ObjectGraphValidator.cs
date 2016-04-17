

//
//  ObjectGraphValidator.cs
//  DotNetObjectGraphValidator
//
//  Copyright 2016 Kenji Nishishiro. All rights reserved.
//  Written by Kenji Nishishiro <marvel@programmershigh.org>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Marvelph.ObjectGraphValidator
{
    public class ValidateException : Exception
    {
        public ValidateException(string message, Schema schema, object objectGraph)
            : base(message)
        {
            this.Schema = schema;
            this.ObjectGraph = objectGraph;
        }

        public Schema Schema { get; }
        public object ObjectGraph { get; }
    }

    public class CompositValidateException : ValidateException
    {
        public CompositValidateException(string message, Schema schema, object objectGraph, ValidateException[] exceptions)
            : base(message, schema, objectGraph)
        {
            if (exceptions == null)
            {
                throw new ArgumentNullException();
            }

            this.Exceptions = exceptions;
        }

        public ValidateException[] Exceptions { get; }
    }

    public abstract class Schema
    {
        public abstract object Validate(object objectGraph, bool extra = false);
    }

    public abstract class Marker : Schema
    {
        public Marker(Schema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException();
            }

            this.schema = schema;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            return this.schema.Validate(objectGraph, extra);
        }

        private Schema schema;
    }

    public class OptionalMarker : Marker
    {
        public OptionalMarker(Schema schema)
            : base(schema)
        {
        }
    }

    public abstract class Type : Schema
    {
    }

    public abstract class Type<T> : Type
    {
        public override object Validate(object objectGraph, bool extra = false)
        {
            if (objectGraph != null)
            {
                if (objectGraph is T)
                {
                    return objectGraph;
                }
                else
                {
                    throw new ValidateException("Type mismatch.", this, objectGraph);
                }
            }
            else
            {
                return null;
            }
        }
    }

    public class StringType : Type<string>
    {
    }

    public class IntType : Type<int>
    {
    }

    public class LongType : Type
    {
        public override object Validate(object objectGraph, bool extra = false)
        {
            if (objectGraph != null)
            {
                if (objectGraph is int)
                {
                    return (long)(int)objectGraph;
                }
                else if (objectGraph is long)
                {
                    return objectGraph;
                }
                else
                {
                    throw new ValidateException("Type mismatch.", this, objectGraph);
                }
            }
            else
            {
                return null;
            }
        }
    }

    public class DecimalType : Type
    {
        public override object Validate(object objectGraph, bool extra = false)
        {
            if (objectGraph != null)
            {
                if (objectGraph is int)
                {
                    return (decimal)(int)objectGraph;
                }
                else if (objectGraph is long)
                {
                    return (decimal)(long)objectGraph;
                }
                else if (objectGraph is decimal)
                {
                    return objectGraph;
                }
                else
                {
                    throw new ValidateException("Type mismatch.", this, objectGraph);
                }
            }
            else
            {
                return null;
            }
        }
    }

    public class DoubleType : Type
    {
        public override object Validate(object objectGraph, bool extra = false)
        {
            if (objectGraph != null)
            {
                if (objectGraph is int)
                {
                    return (double)(int)objectGraph;
                }
                else if (objectGraph is long)
                {
                    return (double)(long)objectGraph;
                }
                else if (objectGraph is decimal)
                {
                    return (double)(decimal)objectGraph;
                }
                else if (objectGraph is double)
                {
                    return objectGraph;
                }
                else
                {
                    throw new ValidateException("Type mismatch.", this, objectGraph);
                }
            }
            else
            {
                return null;
            }
        }
    }

    public class BoolType : Type<bool>
    {
    }

    public class ListType : Type
    {
        public ListType(Schema schema)
        {
            this.schema = schema;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            if (objectGraph != null)
            {
                if (objectGraph is IList<object>)
                {
                    List<object> normalized = new List<object>();
                    foreach (object item in (IList<object>)objectGraph)
                    {
                        normalized.Add(this.schema.Validate(item, extra));
                    }
                    return normalized.ToArray<object>();
                }
                else
                {
                    throw new ValidateException("Type mismatch.", this, objectGraph);
                }
            }
            else
            {
                return null;
            }
        }

        private Schema schema;
    }

    public class DictionaryType : Type
    {
        public DictionaryType(Dictionary<string, Schema> schemas)
        {
            if (schemas == null)
            {
                throw new ArgumentNullException();
            }

            this.schemas = schemas;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            if (objectGraph != null)
            {
                if (objectGraph is IDictionary<string, object>)
                {
                    foreach (KeyValuePair<string, Schema> pair in this.schemas)
                    {
                        if (!((IDictionary<string, object>)objectGraph).ContainsKey(pair.Key))
                        {
                            if (!(pair.Value is OptionalMarker))
                            {
                                throw new ValidateException("Missing key.", this, objectGraph);
                            }
                        }
                    }
                    Dictionary<string, object> normalized = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, object> pair in (IDictionary<string, object>)objectGraph)
                    {
                        if (this.schemas.ContainsKey(pair.Key))
                        {
                            normalized.Add(pair.Key, this.schemas[pair.Key].Validate(pair.Value, extra));
                        }
                        else if (!extra)
                        {
                            throw new ValidateException("Extra key.", this, objectGraph);
                        }
                    }
                    return normalized;
                }
                else
                {
                    throw new ValidateException("Type mismatch.", this, objectGraph);
                }
            }
            else
            {
                return null;
            }
        }

        private Dictionary<string, Schema> schemas;
    }

    public abstract class Modifier : Schema
    {
        public Modifier(Schema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException();
            }

            this.schema = schema;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            return this.schema.Validate(objectGraph, extra);
        }

        private Schema schema;
    }

    public class RequiredModifier : Modifier
    {
        public RequiredModifier(Schema schema)
            : base(schema)
        {
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph == null)
            {
                throw new ValidateException("Required value.", this, objectGraph);
            }
            return objectGraph;
        }
    }

    public class EqualModifier : Modifier
    {
        public EqualModifier(Schema schema, string value)
            : base(schema)
        {
            this.modifier = new EqualModifier<string>(schema, value);
        }

        public EqualModifier(Schema schema, int value)
            : base(schema)
        {
            this.modifier = new EqualModifier<int>(schema, value);
        }

        public EqualModifier(Schema schema, long value)
            : base(schema)
        {
            this.modifier = new EqualModifier<long>(schema, value);
        }

        public EqualModifier(Schema schema, decimal value)
            : base(schema)
        {
            this.modifier = new EqualModifier<decimal>(schema, value);
        }

        public EqualModifier(Schema schema, double value)
            : base(schema)
        {
            this.modifier = new EqualModifier<double>(schema, value);
        }

        public EqualModifier(Schema schema, bool value)
            : base(schema)
        {
            this.modifier = new EqualModifier<bool>(schema, value);
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            return this.modifier.Validate(objectGraph, extra);
        }

        private Modifier modifier;
    }

    internal class EqualModifier<T> : Modifier
    {
        public EqualModifier(Schema schema, T value)
            : base(schema)
        {
            this.value = value;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is T)
                {
                    if (!((T)objectGraph).Equals(this.value))
                    {
                        throw new ValidateException("Not equal.", this, objectGraph);

                    }
                }
                else
                {
                    throw new ValidateException("Can't equal.", this, objectGraph);
                }
            }
            return objectGraph;
        }

        private T value;
    }

    public class EnumModifier : Modifier
    {
        public EnumModifier(Schema schema, params string[] values)
            : base(schema)
        {
            this.modifier = new EnumModifier<string>(schema, values);
        }

        public EnumModifier(Schema schema, params int[] values)
            : base(schema)
        {
            this.modifier = new EnumModifier<int>(schema, values);
        }

        public EnumModifier(Schema schema, params long[] values)
            : base(schema)
        {
            this.modifier = new EnumModifier<long>(schema, values);
        }

        public EnumModifier(Schema schema, params decimal[] values)
            : base(schema)
        {
            this.modifier = new EnumModifier<decimal>(schema, values);
        }

        public EnumModifier(Schema schema, params double[] values)
            : base(schema)
        {
            this.modifier = new EnumModifier<double>(schema, values);
        }

        public EnumModifier(Schema schema, params bool[] values)
            : base(schema)
        {
            this.modifier = new EnumModifier<bool>(schema, values);
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            return this.modifier.Validate(objectGraph, extra);
        }

        private Modifier modifier;
    }

    internal class EnumModifier<T> : Modifier
    {
        public EnumModifier(Schema schema, params T[] values)
            : base(schema)
        {
            if (values == null)
            {
                throw new ArgumentNullException();
            }

            this.values = values;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is T)
                {
                    if (!this.values.Contains((T)objectGraph))
                    {
                        throw new ValidateException("Not enum constant.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't check enum.", this, objectGraph);
                }
            }
            return objectGraph;
        }

        private T[] values;
    }

    public class LengthModifier : Modifier
    {
        public LengthModifier(Schema schema, int? min = null, int? max = null)
            : base(schema)
        {
            this.min = min;
            this.max = max;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is string)
                {
                    if (this.min != null && ((string)objectGraph).Length < this.min)
                    {
                        throw new ValidateException("Too short.", this, objectGraph);
                    }
                    else if (this.max != null && ((string)objectGraph).Length > this.max)
                    {
                        throw new ValidateException("Too long.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't check length.", this, objectGraph);
                }
            }
            return objectGraph;
        }

        private int? min;
        private int? max;
    }

    public class AsciiModifier : Modifier
    {
        public AsciiModifier(Schema schema)
            : base(schema)
        {
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is string)
                {
                    if (Encoding.UTF8.GetByteCount((string)objectGraph) != ((string)objectGraph).Length)
                    {
                        throw new ValidateException("Not ascii.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't check ascii.", this, objectGraph);
                }
            }
            return objectGraph;
        }
    }

    public class MatchModifier : Modifier
    {
        public MatchModifier(Schema schema, string pattern)
            : base(schema)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException();
            }

            this.regex = new Regex(pattern);
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is string)
                {
                    if (!this.regex.IsMatch((string)objectGraph))
                    {
                        throw new ValidateException("Don't match.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't match.", this, objectGraph);
                }
            }
            return objectGraph;
        }

        private Regex regex;
    }

    public class RangeModifier : Modifier
    {
        public RangeModifier(Schema schema, int? min = null, int? max = null, bool minIncluded = true, bool maxIncluded = true)
            : base(schema)
        {
            this.modifier = new RangeModifier<int>(schema, min, max, minIncluded, maxIncluded);
        }

        public RangeModifier(Schema schema, long? min = null, long? max = null, bool minIncluded = true, bool maxIncluded = true)
            : base(schema)
        {
            this.modifier = new RangeModifier<long>(schema, min, max, minIncluded, maxIncluded);
        }

        public RangeModifier(Schema schema, decimal? min = null, decimal? max = null, bool minIncluded = true, bool maxIncluded = true)
            : base(schema)
        {
            this.modifier = new RangeModifier<decimal>(schema, min, max, minIncluded, maxIncluded);
        }

        public RangeModifier(Schema schema, double? min = null, double? max = null, bool minIncluded = true, bool maxIncluded = true)
            : base(schema)
        {
            this.modifier = new RangeModifier<double>(schema, min, max, minIncluded, maxIncluded);
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            return this.modifier.Validate(objectGraph, extra);
        }

        private Modifier modifier;
    }

    internal class RangeModifier<T> : Modifier where T : struct, IComparable<T>
    {
        public RangeModifier(Schema schema, T? min, T? max, bool minIncluded, bool maxIncluded)
            : base(schema)
        {
            this.min = min;
            this.max = max;
            this.minIncluded = minIncluded;
            this.maxIncluded = maxIncluded;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is T)
                {
                    if (this.min != null && minIncluded && ((T)objectGraph).CompareTo((T)this.min) < 0)
                    {
                        throw new ValidateException("Too small.", this, objectGraph);
                    }
                    else if (this.min != null && !minIncluded && ((T)objectGraph).CompareTo((T)this.min) <= 0)
                    {
                        throw new ValidateException("Too small.", this, objectGraph);
                    }
                    else if (this.max != null && maxIncluded && ((T)objectGraph).CompareTo((T)this.max) > 0)
                    {
                        throw new ValidateException("Too large.", this, objectGraph);
                    }
                    else if (this.max != null && !maxIncluded && ((T)objectGraph).CompareTo((T)this.max) >= 0)
                    {
                        throw new ValidateException("Too large.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't compare.", this, objectGraph);
                }
            }
            return objectGraph;
        }

        private T? min;
        private T? max;
        bool minIncluded;
        bool maxIncluded;
    }

    public class CountModifier : Modifier
    {
        public CountModifier(Schema schema, int? min = null, int? max = null)
            : base(schema)
        {
            this.min = min;
            this.max = max;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is IList<object>)
                {
                    if (this.min != null && ((IList<object>)objectGraph).Count < this.min)
                    {
                        throw new ValidateException("Too short.", this, objectGraph);
                    }
                    else if (this.max != null && ((IList<object>)objectGraph).Count > this.max)
                    {
                        throw new ValidateException("Too long.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't count.", this, objectGraph);
                }
            }
            return objectGraph;
        }

        private int? min;
        private int? max;
    }

    public class ConvertModifier : Modifier
    {
        public ConvertModifier(Schema schema, Func<string, object> func)
            : base(schema)
        {
            if (func == null)
            {
                throw new ArgumentNullException();
            }

            this.func = func;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is string)
                {
                    object normalized = this.func((string)objectGraph);
                    if (normalized != null)
                    {
                        return normalized;
                    }
                    else
                    {
                        throw new ValidateException("Don't convert.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't convert.", this, objectGraph);
                }
            }
            return null;
        }

        private Func<string, object> func;
    }

    public class ValidateModifier : Modifier
    {
        public ValidateModifier(Schema schema, Func<string, bool> func)
            : base(schema)
        {
            if (func == null)
            {
                throw new ArgumentNullException();
            }

            this.func = func;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            objectGraph = base.Validate(objectGraph, extra);
            if (objectGraph != null)
            {
                if (objectGraph is string)
                {
                    if (this.func((string)objectGraph))
                    {
                        return objectGraph;
                    }
                    else
                    {
                        throw new ValidateException("Don't validate.", this, objectGraph);
                    }
                }
                else
                {
                    throw new ValidateException("Can't validate.", this, objectGraph);
                }
            }
            return null;
        }

        private Func<string, bool> func;
    }

    public abstract class Operator : Schema
    {
    }

    public class AnyOperator : Operator
    {
        public AnyOperator(params Schema[] schemas)
        {
            if (schemas == null)
            {
                throw new ArgumentNullException();
            }

            this.schemas = schemas;
        }

        public override object Validate(object objectGraph, bool extra = false)
        {
            List<ValidateException> exceptions = new List<ValidateException>();
            foreach (Schema schema in this.schemas)
            {
                try
                {
                    return schema.Validate(objectGraph, extra);
                }
                catch (ValidateException exception)
                {
                    exceptions.Add(exception);
                }
            }
            throw new CompositValidateException("All failure.", this, objectGraph, exceptions.ToArray());
        }

        private Schema[] schemas;
    }
}
