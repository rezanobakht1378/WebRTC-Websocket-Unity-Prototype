using System;
using System.Text;

namespace SimpleJSON
{
	public class JSONNumber : JSONNode
	{
		private double m_Data;

		public override JSONNodeType Tag => JSONNodeType.Number;

		public override bool IsNumber => true;

		public override string Value
		{
			get
			{
				return m_Data.ToString();
			}
			set
			{
				if (double.TryParse(value, out var result))
				{
					m_Data = result;
				}
			}
		}

		public override double AsDouble
		{
			get
			{
				return m_Data;
			}
			set
			{
				m_Data = value;
			}
		}

		public override Enumerator GetEnumerator()
		{
			return default(Enumerator);
		}

		public JSONNumber(double aData)
		{
			m_Data = aData;
		}

		public JSONNumber(string aData)
		{
			Value = aData;
		}

		internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
		{
			aSB.Append(m_Data);
		}

		private static bool IsNumeric(object value)
		{
			if (!(value is int) && !(value is uint) && !(value is float) && !(value is double) && !(value is decimal) && !(value is long) && !(value is ulong) && !(value is short) && !(value is ushort) && !(value is sbyte))
			{
				return value is byte;
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (base.Equals(obj))
			{
				return true;
			}
			JSONNumber jSONNumber = obj as JSONNumber;
			if (jSONNumber != null)
			{
				return m_Data == jSONNumber.m_Data;
			}
			if (IsNumeric(obj))
			{
				return Convert.ToDouble(obj) == m_Data;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_Data.GetHashCode();
		}
	}
}
