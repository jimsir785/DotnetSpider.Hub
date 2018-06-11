﻿using System;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using DotnetSpider.Hub.Agent.Process;
using Serilog;

namespace DotnetSpider.Hub.Agent.Command
{
	public class Cancel : Command
	{
		public override string Name => CommandNames.CancelName;

		public override void Execute(Messsage command, AgentClient client)
		{
			if (!ProcessManager.IsTaskExsits(command.TaskId))
			{
				Log.Logger.Warning($"Task {command.TaskId} is not running.");
				return;
			}

			ProcessInfo processInfo = ProcessManager.GetProcessDetail(command.TaskId);
			if (processInfo != null)
			{
				var process = processInfo.Process;
				try
				{
					SendExitSignal(command.TaskId.ToString(), processInfo.WorkingDirectory);
				}
				catch
				{
					//ignore
				}
				process.WaitForExit(30000);

				try
				{
					process.Kill();
				}
				catch (NotSupportedException nse)
				{
					Log.Logger.Information($"Kill task {command.TaskId} success: {nse.Message}.");
				}
				catch (Win32Exception we)
				{
					Log.Logger.Information($"Kill task {command.TaskId} success: {we.Message}.");
				}
				catch (InvalidOperationException ioe)
				{
					Log.Logger.Information($"Kill task {command.TaskId} success: {ioe.Message}.");
				}
				catch (Exception e)
				{
					Log.Logger.Error($"Kill task {command.TaskId} failed: {e}.");
				}
			}
		}

		private void SendExitSignal(string taskId, string workdirectory)
		{
			if (Env.IsRunningOnWindows)
			{
				var taskIdMmf = MemoryMappedFile.OpenExisting(taskId, MemoryMappedFileRights.Write);
				using (MemoryMappedViewStream stream = taskIdMmf.CreateViewStream())
				{
					var writer = new BinaryWriter(stream);
					writer.Write(1);
				}
			}
			else
			{
				File.Create(Path.Combine(workdirectory, $"{taskId}_cl"));
			}
		}
	}
}
