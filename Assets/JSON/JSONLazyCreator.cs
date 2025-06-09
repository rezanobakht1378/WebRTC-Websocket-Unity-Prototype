using System.Text;

namespace SimpleJSON
{
	internal class JSONLazyCreator : JSONNode
	{
		private JSONNode m_Node;

		private string m_Key;

		public override JSONNodeType Tag => JSONNodeType.None;

		public override JSONNode this[int aIndex]
		{
			get
			{
				return new JSONLazyCreator(this);
			}
			set
			{
				JSONArray jSONArray = new JSONArray();
				jSONArray.Add(value);
				Set(jSONArray);
			}
		}

		public override JSONNode this[string aKey]
		{
			get
			{
				return new JSONLazyCreator(this, aKey);
			}
			set
			{
				JSONObject jSONObject = new JSONObject();
				jSONObject.Add(aKey, value);
				Set(jSONObject);
			}
		}

		public override int AsInt
		{
			get
			{
				JSONNumber aVal = new JSONNumber(0.0);
				Set(aVal);
				return 0;
			}
			set
			{
				JSONNumber aVal = new JSONNumber(value);
				Set(aVal);
			}
		}

		public override float AsFloat
		{
			get
			{
				JSONNumber aVal = new JSONNumber(0.0);
				Set(aVal);
				return 0f;
			}
			set
			{
				JSONNumber aVal = new JSONNumber(value);
				Set(aVal);
			}
		}

		public override double AsDouble
		{
			get
			{
				JSONNumber aVal = new JSONNumber(0.0);
				Set(aVal);
				return 0.0;
			}
			set
			{
				JSONNumber aVal = new JSONNumber(value);
				Set(aVal);
			}
		}

		public override bool AsBool
		{
			get
			{
				JSONBool aVal = new JSONBool(aData: false);
				Set(aVal);
				return false;
			}
			set
			{
				JSONBool aVal = new JSONBool(value);
				Set(aVal);
			}
		}

		public override JSONArray AsArray
		{
			get
			{
				JSONArray jSONArray = new JSONArray();
				Set(jSONArray);
				return jSONArray;
			}
		}

		public override JSONObject AsObject
		{
			get
			{
				JSONObject jSONObject = new JSONObject();
				Set(jSONObject);
				return jSONObject;
			}
		}

		public override Enumerator GetEnumerator()
		{
			return default(Enumerator);
		}

		public JSONLazyCreator(JSONNode aNode)
		{
			m_Node = aNode;
			m_Key = null;
		}

		public JSONLazyCreator(JSONNode aNode, string aKey)
		{
			m_Node = aNode;
			m_Key = aKey;
		}

		private void Set(JSONNode aVal)
		{
			if (m_Key == null)
			{
				m_Node.Add(aVal);
			}
			else
			{
				m_Node.Add(m_Key, aVal);
			}
			m_Node = null;
		}

		public override void Add(JSONNode aItem)
		{
			JSONArray jSONArray = new JSONArray();
			jSONArray.Add(aItem);
			Set(jSONArray);
		}

		public override void Add(string aKey, JSONNode aItem)
		{
			JSONObject jSONObject = new JSONObject();
			jSONObject.Add(aKey, aItem);
			Set(jSONObject);
		}

		public static bool operator ==(JSONLazyCreator a, object b)
		{
			if (b == null)
			{
				return true;
			}
			return (object)a == b;
		}

		public static bool operator !=(JSONLazyCreator a, object b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return true;
			}
			return (object)this == obj;
		}

		public override int GetHashCode()
		{
			return 0;
		}

		internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
		{
			aSB.Append("null");
		}
	}
}
