﻿using RapidCMS.Common.Attributes;

namespace RapidCMS.Common.ValueMappers
{
    [DefaultType(typeof(int))]
    public class IntValueMapper : ValueMapper<int>
    {
        public override int MapFromEditor(object value)
        {
            if (value is string stringValue)
            {
                return int.TryParse(stringValue, out var boolValue) ? boolValue : default;
            }
            else if (value is int intValue)
            {
                return intValue;
            }
            else
            {
                return default;
            }
        }

        public override object MapToEditor(int value)
        {
            return value;
        }
    }
}
