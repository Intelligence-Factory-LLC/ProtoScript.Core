using BasicUtilities;
using BasicUtilities.Collections;
using Ontology;
using Ontology.Simulation;
using Ontology.Utils;
using ProtoScript.Extensions.SolutionItems;
using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Parsers;
using RooTrax.Cache;
using System.Collections.Concurrent;
using System.Text;
using WebAppUtilities;

namespace ProtoScript.Extensions
{
	public class Diagnostic
	{
		public string Type = string.Empty;
		public string Message = string.Empty;
		public StatementParsingInfo? Info;
	}

	public class ProtoScriptWorkbench : JsonWs
	{
		static protected ConcurrentDictionary<string, SessionObject> m_mapSessions = new ConcurrentDictionary<string, SessionObject>();
		static private string? _webRoot;
		static public Debugger? m_Debugger;
		static public Sockets m_Sockets = new Sockets();

		static public void SetWebRoot(string path)
		{
			_webRoot = path;
		}

		static public string GetWebRoot()
		{
			return _webRoot ?? string.Empty;
		}

		static private string EnsureAbsolutePath(string path)
		{
			if (!System.IO.Path.IsPathRooted(path) && !string.IsNullOrEmpty(_webRoot))
			{
				return System.IO.Path.Combine(_webRoot, path);
			}
			return path;
		}

		static public SessionObject GetOrCreateSession(string strProject)
		{
			strProject = EnsureAbsolutePath(strProject);

			if (StringUtil.IsEmpty(strProject) || !m_mapSessions.TryGetValue(strProject, out SessionObject? session))
			{
				return CreateSession(strProject);
			}

			EnterSession(session);
			return session;
		}

		static public void Reset()
		{
			m_mapSessions.Clear();
			m_Debugger = null;
			m_Sockets = new Sockets();
		}

		static protected void EnterSession(SessionObject session)
		{
			CacheManager.UseAsyncLocal = true;
			ObjectCacheManager.UseAsyncLocal = true;
			TemporaryPrototypes.Cache.InsertLogFrequency = 10000;
			session.Enter();
			Ontology.Initializer.ReloadCache();
		}

		static private SessionObject CreateSession(string strSessionKey)
		{
			SessionObject session = SessionObject.Create(strSessionKey);
			EnterSession(session);
			m_mapSessions[strSessionKey] = session;
			return session;
		}

		static public string LoadFile(string strSessionKey, string strFile)
		{
			try
			{
				Logs.DebugLog.WriteEvent("ProtoScriptWorkbench.LoadFile.Start", $"{strSessionKey} -> {strFile}");
				SessionObject session = GetOrCreateSession(strSessionKey);
				string strContents = FileUtil.ReadFile(strFile);
				session.Context.SetCurrentFile(strFile);
				Logs.DebugLog.WriteEvent("ProtoScriptWorkbench.LoadFile.Stop", strFile);
				return strContents;
			}
			catch (Exception err)
			{
				Logs.LogError(err);
				throw;
			}
		}

		public static bool CreateNewFile(string strProject, string strNewFile)
		{
			strProject = EnsureAbsolutePath(strProject);

			string rootDir = StringUtil.LeftOfLast(strProject, "\\");
			string finalPath;
			if (System.IO.Path.IsPathRooted(strNewFile))
			{
				finalPath = strNewFile;
			}
			else
			{
				finalPath = FileUtil.BuildPath(rootDir, strNewFile);
			}

			if (System.IO.File.Exists(finalPath))
			{
				throw new JsonWsException($"File already exists: {finalPath}");
			}

			try
			{
				string directoryPath = StringUtil.LeftOfLast(finalPath, "\\");
				if (!System.IO.Directory.Exists(directoryPath))
				{
					throw new JsonWsException($"Directory does not exist: {directoryPath}");
				}

				FileUtil.WriteFile(finalPath, $"//{strNewFile}\n");
				FileUtil.AppendFile(strProject, $"include \"{strNewFile}\";");
			}
			catch (Exception ex)
			{
				throw new JsonWsException($"Failed to create file: {ex.Message}");
			}

			return true;
		}

		static public List<string> LoadProject(string strProject)
		{
			try
			{
				Logs.DebugLog.WriteEvent("ProtoScriptWorkbench.LoadProject.Start", strProject);
				SessionObject session = GetOrCreateSession(strProject);
				List<string> files = LoadProjectInternal(session);
				Logs.DebugLog.WriteEvent("ProtoScriptWorkbench.LoadProject.Stop", $"{strProject} ({files.Count} files)");
				return files;
			}
			catch (Exception err)
			{
				Logs.LogError(err);
				throw;
			}
		}

		static private List<string> LoadProjectInternal(SessionObject session)
		{
			try
			{
				Logs.RegisterExceptionFormatter(typeof(ProtoScriptTokenizingException), new ProtoScriptTokenizingExceptionFormatter());
				Logs.RegisterExceptionFormatter(typeof(ProtoScriptParsingException), new ProtoScriptParsingExceptionFormatter());
				Logs.RegisterExceptionFormatter(typeof(ProtoScriptCompilerException), new ProtoScriptCompilerExceptionFormatter());

				string strProject = session.SessionKey;
				ProtoScript.File fileProject = ProtoScript.Parsers.Files.Parse(strProject);
				string strRootDir = StringUtil.LeftOfLast(strProject, "\\");

				Project project = new Project();
				project.FileName = strProject;
				project.RootDirectory = strRootDir;
				session.Context.Project = project;

				List<ProtoScript.File> lstFiles = Compiler.GetAllIncludedFiles(fileProject, false, true);
				foreach (ProtoScript.File file in lstFiles.OrderBy(x => x.Info.Name))
				{
					long length = 0;
					if (file.Info != null && file.Info.Exists)
					{
						length = file.Info.Length;
					}
					project.Files.Add(new ProtoScriptFile()
					{
						FileName = file.Info.FullName,
						Length = length
					});
				}

				FileInfo infoProject = new FileInfo(strProject);
				long projectLength = 0;
				if (infoProject.Exists)
				{
					projectLength = infoProject.Length;
				}
				project.Files.Add(new ProtoScriptFile()
				{
					FileName = strProject,
					Length = projectLength
				});
				return project.Files.Select(x => x.FileName).ToList();
			}
			catch (ProtoScriptTokenizingException err)
			{
				Logs.LogError(err);
				throw;
			}
		}

		static private Compiler PrepareSessionCompiler(SessionObject session)
		{
			string strProjectName = EnsureAbsolutePath(session.SessionKey);
			session.SessionKey = strProjectName;

			Compiler compiler = new Compiler();
			compiler.ProjectCompilationMode = Compiler.CompilationMode.BestEffort;
			compiler.Initialize();
			List<ProtoScript.Interpretter.Compiled.Statement> statements = compiler.CompileProject(strProjectName);

			session.Context.Compiler = compiler;
			session.Statements = statements;
			session.Debugger = null;
			session.IsInterpretted = false;

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			m_Sockets = new Sockets();
			interpretter.InsertGlobalObject("_sockets", m_Sockets);
			interpretter.InsertGlobalObject("_sessionObject", session);
			session.Interpretter = interpretter;

			return compiler;
		}

		static public void SaveCurrentCode(string strSessionKey, string strFile, string Code)
		{
			string strFileName = StringUtil.RightOfLast(strFile, "\\");

			if (StringUtil.IsEmpty(Code))
			{
				throw new JsonWsException("Empty code being saved");
			}

			for (int i = 50; i > 0; i--)
			{
				string strSourceFile = FileUtil.BuildPath(@"C:\temp\protoscriptscript_back", strFileName + "_" + (i - 1));
				if (System.IO.File.Exists(strSourceFile))
				{
					string strTargetFile = FileUtil.BuildPath(@"C:\temp\protoscriptscript_back", strFileName + "_" + i);
					System.IO.File.Copy(strSourceFile, strTargetFile, true);
				}
			}

			string strBackup = FileUtil.BuildPath(@"C:\temp\protoscriptscript_back", strFileName + "_0");
			FileUtil.WriteFile(strBackup, FileUtil.ReadFile(strFile));
			FileUtil.WriteFile(strFile, Code);
			Logs.DebugLog.WriteEvent("ProtoScriptWorkbench.SaveCurrentCode", strFile);
		}

		static public Diagnostic? ParseCode(string Code)
		{
			try
			{
				ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(Code);
			}
			catch (ProtoScriptTokenizingException err)
			{
				return new Diagnostic()
				{
					Type = "Parsing",
					Message = BuildParserDiagnosticMessage(err),
					Info = new StatementParsingInfo() { StartingOffset = err.Cursor }
				};
			}

			return null;
		}

		[JsonWsSerialize(JsonWs.SerializeResultsOptions.Full)]
		static public List<Diagnostic> CompileCode(string strCode)
		{
			List<Diagnostic> lstDiagnostics = new List<Diagnostic>();
			Compiler compiler = new Compiler();
			compiler.Initialize();

			try
			{
				ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(strCode);
				compiler.Compile(file);
				lstDiagnostics = GetCompilerDiagnostics(compiler);
			}
			catch (ProtoScriptTokenizingException err)
			{
				lstDiagnostics.Add(new Diagnostic()
				{
					Type = "Parsing",
					Message = BuildParserDiagnosticMessage(err),
					Info = new StatementParsingInfo() { StartingOffset = err.Cursor, Length = 1, File = err.File }
				});
			}
			catch (ProtoScriptCompilerException err)
			{
				lstDiagnostics.Add(CreateCompilerExceptionDiagnostic(err));
			}
			catch (Exception err)
			{
				Logs.LogError(err);
				lstDiagnostics.Add(CreateUnhandledCompileDiagnostic(err));
			}

			return lstDiagnostics;
		}

		[JsonWsSerialize(JsonWs.SerializeResultsOptions.Full)]
		static public List<Diagnostic> CompileCodeWithProject(string strCode, string strProjectName)
		{
			strProjectName = EnsureAbsolutePath(strProjectName);
			SessionObject session = GetOrCreateSession(strProjectName);
			List<Diagnostic> lstDiagnostics = new List<Diagnostic>();

			try
			{
				LoadProjectInternal(session);
				Compiler compiler = PrepareSessionCompiler(session);
				lstDiagnostics = GetCompilerDiagnostics(compiler);
			}
			catch (ProtoScriptTokenizingException err)
			{
				lstDiagnostics.Add(new Diagnostic()
				{
					Type = "Parsing",
					Message = BuildParserDiagnosticMessage(err),
					Info = new StatementParsingInfo() { StartingOffset = err.Cursor, Length = 1, File = err.File }
				});
			}
			catch (ProtoScriptCompilerException err)
			{
				if (session.Context.Compiler != null)
				{
					lstDiagnostics = GetCompilerDiagnostics(session.Context.Compiler);
				}
				if (lstDiagnostics.Count == 0)
				{
					lstDiagnostics.Add(CreateCompilerExceptionDiagnostic(err));
				}
			}
			catch (Exception err)
			{
				Logs.LogError(err);
				if (session.Context.Compiler != null)
				{
					lstDiagnostics = GetCompilerDiagnostics(session.Context.Compiler);
				}
				if (lstDiagnostics.Count == 0)
				{
					lstDiagnostics.Add(CreateUnhandledCompileDiagnostic(err));
				}
			}

			return lstDiagnostics;
		}

		static private List<Diagnostic> GetCompilerDiagnostics(Compiler compiler)
		{
			List<Diagnostic> lstDiagnostics = new List<Diagnostic>();
			foreach (CompilerDiagnostic compilerDiagnostic in compiler.Diagnostics)
			{
				Diagnostic diagnostic = new Diagnostic();
				diagnostic.Type = "Compiler";
				diagnostic.Message = compilerDiagnostic.Diagnostic.Message;
				if (compilerDiagnostic.Statement != null)
				{
					diagnostic.Info = compilerDiagnostic.Statement.Info;
					diagnostic.Message += " - " + SimpleGenerator.Generate(compilerDiagnostic.Statement);
				}
				else if (compilerDiagnostic.Expression != null)
				{
					diagnostic.Info = compilerDiagnostic.Expression.Info;
					diagnostic.Message += " - " + SimpleGenerator.Generate(compilerDiagnostic.Expression);
				}

				lstDiagnostics.Add(diagnostic);
			}
			return lstDiagnostics;
		}

		private static Diagnostic CreateCompilerExceptionDiagnostic(ProtoScriptCompilerException err)
		{
			string message = !string.IsNullOrWhiteSpace(err.Explanation) ? err.Explanation : err.Message;
			return new Diagnostic
			{
				Type = "Compiler",
				Message = message,
				Info = err.Info
			};
		}

		private static Diagnostic CreateUnhandledCompileDiagnostic(Exception err)
		{
			Exception root = err;
			while (root.InnerException != null)
			{
				root = root.InnerException;
			}

			return new Diagnostic
			{
				Type = "Compiler",
				Message = "Compilation failed: " + root.Message
			};
		}

		private static string BuildParserDiagnosticMessage(ProtoScriptTokenizingException err)
		{
			if (!string.IsNullOrWhiteSpace(err.Explanation))
			{
				return err.Explanation;
			}

			if (!string.IsNullOrWhiteSpace(err.Expected))
			{
				return "Expected: " + err.Expected;
			}

			return err.Message;
		}

		public class Symbol
		{
			public string SymbolName = string.Empty;
			public string SymbolType = string.Empty;
			public StatementParsingInfo? Info;
		}

		static public List<Symbol> GetSymbols(string strSessionKey)
		{
			try
			{
				List<Symbol> lstSymbols = new List<Symbol>();
				SessionObject session = GetOrCreateSession(strSessionKey);
				foreach (ProtoScript.Interpretter.Compiled.Statement statement in session.Statements)
				{
					if (statement is ProtoScript.Interpretter.Compiled.PrototypeDeclaration declaration)
					{
						lstSymbols.Add(new Symbol
						{
							SymbolName = declaration.PrototypeName,
							SymbolType = "PrototypeDeclaration",
							Info = declaration.Info
						});
					}
					else if (statement is FunctionRuntimeInfo functionInfo)
					{
						lstSymbols.Add(new Symbol
						{
							SymbolName = functionInfo.FunctionName,
							SymbolType = "FunctionRuntimeInfo",
							Info = functionInfo.Info
						});
					}
				}

				return lstSymbols.OrderBy(x => x.SymbolName).ToList();
			}
			catch (Exception err)
			{
				throw new JsonWsException(err.Message);
			}
		}

		static public List<CodeContext.Symbol> GetSymbolsAtCursor(string strSessionKey, string strFileName, int iPos)
		{
			SessionObject session = GetOrCreateSession(strSessionKey);
			List<CodeContext.Symbol> lstSymbols = new List<CodeContext.Symbol>();

			if (session.Context.Compiler != null)
			{
				try
				{
					session.Context.SetCurrentFile(strFileName);
					lstSymbols = session.Context.GetSymbolsAtCursor(iPos);
				}
				catch (Exception err)
				{
					throw new JsonWsException(err.Message);
				}
			}

			return lstSymbols;
		}

		static public List<CodeContext.Symbol> Suggest(string strSessionKey, string strLine, int iPos)
		{
			SessionObject session = GetOrCreateSession(strSessionKey);
			List<CodeContext.Symbol> lstSymbols = new List<CodeContext.Symbol>();

			if (session.Context.Compiler != null)
			{
				try
				{
					lstSymbols = session.Context.Suggest(iPos, strLine);
				}
				catch (Exception err)
				{
					throw new JsonWsException(err.Message);
				}
			}

			return lstSymbols;
		}

		static public List<string> PredictNextLine(string strSessionKey, int iPos)
		{
			SessionObject session = GetOrCreateSession(strSessionKey);
			return session.Context.PredictNextLine(iPos);
		}

		static public string GetSymbolInfo(string strSessionKey, string strSymbol, StatementParsingInfo info)
		{
			SessionObject session = GetOrCreateSession(strSessionKey);
			StringBuilder sb = new StringBuilder();

			if (session.Context.Compiler == null)
			{
				return string.Empty;
			}

			object? obj = session.Context.Compiler.Symbols.GetGlobalScope().GetSymbol(strSymbol);
			if (obj is PrototypeTypeInfo infoType)
			{
				Prototype prototype = infoType.Prototype;
				sb.AppendLine("Prototype: " + prototype.PrototypeName);
				sb.AppendLine("PrototypeID: " + prototype.PrototypeID);
				sb.AppendLine();
				sb.AppendLine("Symbols:");
				foreach (KeyValuePair<string, object> pair in infoType.Scope)
				{
					sb.Append(pair.Key).Append(" = ").AppendLine(pair.Value.GetType().ToString());
				}

				sb.AppendLine();
				sb.AppendLine("Children:");
				foreach (KeyValuePair<string, object> pair in session.Context.Compiler.Symbols.GetGlobalScope())
				{
					if (pair.Value is PrototypeTypeInfo childType)
					{
						Prototype protoChild = childType.Prototype;
						if (protoChild.TypeOf(prototype))
						{
							sb.Append(pair.Key).Append(" = ").AppendLine(protoChild.PrototypeName);
						}
					}
				}
			}
			else if (obj is FunctionRuntimeInfo functionRuntimeInfo)
			{
				sb.AppendLine("Function: " + functionRuntimeInfo.FunctionName);
				sb.AppendLine("Parameters: " + string.Join(", ", functionRuntimeInfo.Parameters.Select(x => x.ParameterName ?? "(null)")));
			}

			return sb.ToString();
		}

		public class TaggingSettings
		{
			public bool AllowRanges = false;
			public bool IncludeTypeOfs = false;
			public int MaxIterations = 1000;
			public string Project = string.Empty;
			public bool IncludeMeaning = false;
			public bool TransferRecursively = false;
			public string? MeaningDimension;
			public bool Debug = false;
			public bool Fragment = false;
			public bool TagAfterFragment = true;
			public bool TagIteratively = false;
			public bool AllowAlreadyLinkedSequences = true;
			public bool PathToStart = true;
			public bool EnableDatabase = false;
			public bool Resume = false;
			public bool AllowPrecompiled = false;
			public string? SessionKey;
			public List<StatementParsingInfo> Breakpoints = new List<StatementParsingInfo>();
		}

		static private bool IsResumable(TaggingSettings settings, SessionObject session)
		{
			bool bResume = settings.Resume && session.Interpretter != null && session.Context.Compiler != null;

			if (bResume && settings.Debug && session.Debugger == null)
			{
				Logs.DebugLog.WriteEvent("Not Resumable", "Debugging");
				bResume = false;
			}
			else if (bResume && session.Context.Project != null)
			{
				Logs.DebugLog.WriteEvent("Not Resumable", "Project has changed on disk");
				bResume = (session.Context.Project.HasProjectChangedOnDisk() == false);
			}
			else if (bResume && !session.IsInterpretted)
			{
				Logs.DebugLog.WriteEvent("Not Resumable", "Session is not interpreted");
				bResume = false;
			}

			return bResume;
		}

		[JsonWsSerialize(JsonWs.SerializeResultsOptions.Full)]
		static public TagImmediateResult InterpretImmediate(string strProject, string strImmediate, TaggingSettings taggingSettings)
		{
			JsonSerializers.RegisterSerializer(typeof(TagImmediateResult), new NestedJsonSerializer<TagImmediateResult>());

			taggingSettings.SessionKey = EnsureAbsolutePath(strProject);
			SessionObject session = GetOrCreateSession(taggingSettings.SessionKey);

			TagImmediateResult result = new TagImmediateResult();
			try
			{
				bool bResume = IsResumable(taggingSettings, session);
				if (!bResume)
				{
					LoadProjectInternal(session);
					PrepareSessionCompiler(session);

					if (session.Interpretter != null)
					{
						session.Interpretter.InterpretStatements(session.Statements);
						session.IsInterpretted = true;
					}
				}

				if (session.Context.Compiler == null || session.Interpretter == null)
				{
					throw new InvalidOperationException($"Could not initialize interpreter session for project '{taggingSettings.SessionKey ?? strProject}'.");
				}

				if (bResume)
				{
					session.Context.Compiler.Diagnostics.Clear();
				}

				ProtoScript.Expression statementImmediate = ProtoScript.Parsers.Expressions.Parse(strImmediate);
				ProtoScript.Interpretter.Compiled.Expression compiledImmediate = session.Context.Compiler.Compile(statementImmediate);

				if (session.Context.Compiler.Diagnostics.Count > 0)
				{
					result.Error = "Error in immediate: " + session.Context.Compiler.Diagnostics.First().Diagnostic.Message;
					return result;
				}

				object? obj;
				if (!taggingSettings.Debug)
				{
					obj = session.Interpretter.Evaluate(compiledImmediate);
				}
				else
				{
					Debugger debugger = session.Debugger ?? new Debugger(session.Context.Compiler);
					session.Debugger = debugger;
					m_Debugger = debugger;

					debugger.Interpretter.Breakpoints.Clear();
					foreach (StatementParsingInfo breakpoint in taggingSettings.Breakpoints)
					{
						debugger.Interpretter.Breakpoints.Add(breakpoint);
					}

					debugger.StartDebugging(compiledImmediate);
					obj = debugger.WaitForEndOfExecution();
				}

				if (obj == null)
				{
					result.Result = "(null)";
				}
				else if (obj is bool boolResult)
				{
					result.Result = boolResult.ToString();
				}
				else if (obj is StringWrapper stringWrapper)
				{
					result.Result = stringWrapper.GetStringValue();
				}
				else if (obj is JsonObject jsonValue)
				{
					result.Result = JsonUtil.ToFriendlyJSON(jsonValue).ToString();
				}
				else if (obj is JsonArray jsonArray)
				{
					result.Result = JsonUtil.ToFriendlyJSON(jsonArray).ToString();
				}
				else if (obj is StringReference stringReference)
				{
					result.Result = stringReference.PrototypeName;
				}
				else
				{
					Prototype? protoValue = session.Interpretter.GetOrConvertToPrototype(obj);
					if (protoValue == null)
					{
						if (obj is ValueRuntimeInfo infoValue)
						{
							result.Result = infoValue.Value == null ? "null" : infoValue.Value.ToString();
						}
						else
						{
							result.Result = obj.ToString();
						}
					}
					else
					{
						PrototypeLogging.IncludeTypeOfs = taggingSettings.IncludeTypeOfs;
						if (Prototypes.TypeOf(protoValue, Ontology.Collection.Prototype) && protoValue.Children.Count == 1)
						{
							result.Result = PrototypeLogging.ToFriendlyString(protoValue.Children[0]).ToString();
						}
						else
						{
							result.Result = PrototypeLogging.ToFriendlyString(protoValue).ToString();
						}
						result.ResultPrototype = protoValue;
					}
				}
			}
			catch (ProtoScriptTokenizingException err)
			{
				result.Error = BuildParserDiagnosticMessage(err);
				result.ErrorStatement = new StatementParsingInfo() { StartingOffset = err.Cursor, Length = 1, File = err.File };
			}
			catch (RuntimeException err)
			{
				result.Error = err.Message;
				result.ErrorStatement = err.Info;
				Exception? innerErr = err.InnerException;
				while (innerErr != null)
				{
					result.Error = innerErr.Message + " - " + result.Error;
					innerErr = innerErr.InnerException;
				}
			}
			catch (ProtoScriptCompilerException err)
			{
				result.Error = err.Message;
				result.ErrorStatement = err.Info;
			}
			catch (Exception err)
			{
				result.Error = err.Message;
			}

			result.ResultPrototype = null;
			return result;
		}

		public class TagImmediateResult
		{
			public string? Result;
			public string? Error;
			public StatementParsingInfo? ErrorStatement;
			public Prototype? ResultPrototype;
		}

		static public void StopTagging(string strSessionKey)
		{
			if (m_Debugger != null)
			{
				m_Debugger.Interpretter.Step = DebuggingInterpretter.StepTypes.Stop;
				m_Debugger.Resume();
			}
		}

		static public JsonObject GetTaggingProgress(string strSessionKey)
		{
			JsonObject jsonObject = new JsonObject();
			jsonObject["Iterations"] = 0;
			jsonObject["CurrentInterpretation"] = "";
			return jsonObject;
		}

		[JsonWsSerialize(JsonWs.SerializeResultsOptions.Full)]
		static public TagImmediateResult TagImmediate(string strFragment, string strProject, TaggingSettings settings)
		{
			return InterpretImmediate(strProject, strFragment, settings);
		}

		static public JsonObject? GetPrototypeAndDescendants(string strSessionKey, string strPrototypeName)
		{
			SessionObject session = GetOrCreateSession(strSessionKey);
			Prototype prototype = TemporaryPrototypes.GetTemporaryPrototype(strPrototypeName);

			JsonObject? jsonRoot = PrototypeLogging.ToFriendlyJsonObject(prototype);
			if (jsonRoot != null)
			{
				JsonArray jsonArray = new JsonArray();
				foreach (Prototype proto in prototype.GetDescendants())
				{
					jsonArray.Add(PrototypeLogging.ToFriendlyJsonObject(proto));
				}
				jsonRoot["Descendants"] = jsonArray;
			}

			return jsonRoot;
		}

		static public List<string> GetPrototypesBySearch(string strSessionKey, string strSearch)
		{
			SessionObject session = GetOrCreateSession(strSessionKey);
			List<Prototype> lstTemporary = TemporaryPrototypes.GetAllTemporaryPrototypes();
			List<string> lstResults = new List<string>();
			int iMax = 100;
			foreach (Prototype proto in lstTemporary)
			{
				if (proto.PrototypeName.StartsWith("System."))
				{
					continue;
				}

				if (StringUtil.InString(proto.PrototypeName, strSearch))
				{
					lstResults.Add(proto.PrototypeName);
				}

				if (lstResults.Count > iMax)
				{
					break;
				}
			}

			return lstResults.OrderBy(x => x).ToList();
		}

		static public string GetSymbol(string strSessionKey, string strSymbol)
		{
			SessionObject session = GetOrCreateSession(strSessionKey);
			if (m_Debugger == null)
			{
				return "Debugger is not active.";
			}

			ProtoScript.Interpretter.Symbols.Scope? scope;
			object? obj = m_Debugger.Interpretter.Symbols.GetSymbolAndScope(strSymbol, out scope);
			if (obj is ValueRuntimeInfo valueInfo && scope != null)
			{
				obj = scope.Stack[valueInfo.Index];
			}

			bool bIncludeTypeOfs = PrototypeLogging.IncludeTypeOfs;
			PrototypeLogging.IncludeTypeOfs = true;
			string strRes;
			Prototype? protoValue = SimpleInterpretter.GetAsPrototype(obj);
			if (protoValue == null)
			{
				if (obj is ValueRuntimeInfo valueRuntimeInfo)
				{
					strRes = valueRuntimeInfo.Value == null ? "(null)" : valueRuntimeInfo.Value.ToString() ?? "(null)";
				}
				else
				{
					strRes = obj == null ? "(null)" : obj.ToString() ?? "(null)";
				}
			}
			else
			{
				strRes = PrototypeLogging.ToFriendlyString(protoValue).ToString();
			}
			PrototypeLogging.IncludeTypeOfs = bIncludeTypeOfs;
			return strRes;
		}

		static public void Resume()
		{
			if (m_Debugger != null)
			{
				m_Debugger.Resume();
			}
		}

		static public StatementParsingInfo? GetBlockedOn()
		{
			if (m_Debugger == null || m_Debugger.Interpretter == null)
			{
				return null;
			}

			return m_Debugger.Interpretter.BlockedOn;
		}

		static public void StepNext()
		{
			if (m_Debugger != null)
			{
				m_Debugger.Interpretter.Step = DebuggingInterpretter.StepTypes.StepNext;
				m_Debugger.Resume();
			}
		}

		static public void StepOver()
		{
			if (m_Debugger != null)
			{
				m_Debugger.Interpretter.Step = DebuggingInterpretter.StepTypes.StepOver;
				m_Debugger.Resume();
			}
		}

		static public void StopDebugging()
		{
			if (m_Debugger != null)
			{
				m_Debugger.Interpretter.Step = DebuggingInterpretter.StepTypes.Stop;
				m_Debugger.Resume();
			}
		}

		static public string GetCallStack()
		{
			if (m_Debugger == null)
			{
				return string.Empty;
			}

			StringBuilder sb = new StringBuilder();
			foreach (string strCall in m_Debugger.Interpretter.CallStack)
			{
				sb.AppendLine(strCall);
			}
			return sb.ToString();
		}

		static public string GetCurrentException()
		{
			if (m_Debugger == null)
			{
				return string.Empty;
			}

			Exception? err = m_Debugger.Interpretter.Exception;
			StringBuilder sb = new StringBuilder();
			while (err != null)
			{
				sb.AppendLine(err.Message);
				err = err.InnerException;
			}
			return sb.ToString();
		}

		static public void Respond(int iMessageID, string strShortForm)
		{
			Prototype prototype = TemporaryPrototypeShortFormParser.FromShortString(strShortForm);
			m_Sockets.Respond(iMessageID, prototype);
		}

		public class MessageResponse
		{
			public Prototype? MessageValue;
			public string MessageText = string.Empty;
			public int MessageID;
		}

		static public MessageResponse? GetNextMessage(int iLastMessageID)
		{
			JsonSerializers.RegisterSerializer(typeof(MessageResponse), new NestedJsonSerializer<MessageResponse>());

			if (m_Sockets != null)
			{
				Sockets.Message? message = m_Sockets.GetNextMessage(iLastMessageID);
				if (message != null)
				{
					MessageResponse messageResponse = new MessageResponse();
					messageResponse.MessageValue = message.MessageValue;
					messageResponse.MessageID = message.MessageID;
					messageResponse.MessageText = PrototypeLogging.ToFriendlyString2(message.MessageValue);
					return messageResponse;
				}
			}
			return null;
		}
	}
}
