using BasicUtilities;
using Ontology;
using ProtoScript.Interpretter;
using ProtoScript.Interpretter.Compiled;
using RooTrax.Cache;
using System.Collections.Concurrent;

namespace ProtoScript.Extensions
{
	public class SessionObject
	{
		private CodeContext m_Context = new CodeContext();
		public CodeContext Context
		{
			get
			{
				return m_Context;
			}

			set
			{
				m_Context = value;
			}
		}

		public string SessionKey = string.Empty;
		public JsonObject Settings = new JsonObject();
		public NativeInterpretter? Interpretter;
		public List<ProtoScript.Interpretter.Compiled.Statement> Statements = new List<ProtoScript.Interpretter.Compiled.Statement>();
		public Debugger? Debugger;
		public bool IsInterpretted;

		public ConcurrentDictionary<string, ObjectCache>? m_objectCache;
		public ConcurrentDictionary<string, RowCache>? m_rowCache;

		public static SessionObject Create(string? strSessionKey = null)
		{
			strSessionKey ??= Guid.NewGuid().ToString();

			SessionObject session = new SessionObject()
			{
				SessionKey = strSessionKey,
			};

			return session;
		}

		public void Enter()
		{
			if (this.m_rowCache == null)
			{
				this.m_rowCache = CacheManager.SetAsyncLocalCache();
				this.m_objectCache = ObjectCacheManager.SetAsyncLocalCache();
			}
			else
			{
				CacheManager.SetAsyncLocalCache(this.m_rowCache);
				ObjectCacheManager.SetAsyncLocalCache(this.m_objectCache);
			}
		}

		public void SetObject<T>(string strKey, T oObj) where T : class
		{
			ObjectCache session = ObjectCacheManager.Instance.GetOrCreateCache("Session");
			session.Insert<T>(oObj, strKey);
		}

		public T GetObject<T>(string strKey) where T : class
		{
			ObjectCache session = ObjectCacheManager.Instance.GetOrCreateCache("Session");
			return session.Get<T>(strKey);
		}
	}
}
