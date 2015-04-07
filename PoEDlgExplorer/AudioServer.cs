using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Threading;

namespace PoEDlgExplorer
{
	public static class AudioServer
	{
		private static SoundPlayer _player = new SoundPlayer();
		private static BlockingCollection<Action> _commandQueue = new BlockingCollection<Action>();
		private static Thread _thread;

		public static void Play(FileInfo file)
		{
			if (_thread == null)
			{
				_thread = new Thread(ThreadWorker);
				_thread.Start();
			}

			_commandQueue.Add(() => { _player.Stream = LoadFile(file); _player.Play(); });
		}

		public static void Stop()
		{
			_commandQueue.Add(() => { _player.Stop(); });
		}

		public static void Kill()
		{
			if (_thread != null)
			{
				_commandQueue.Add(null);
				_thread.Join();
				_thread = null;
			}
		}

		private static void ThreadWorker()
		{
			Action command;
			do
			{
				command = _commandQueue.Take();
				if (command != null)
					command();
			} while (command != null);
		}

		private static MemoryStream LoadFile(FileInfo file)
		{
			var memoryStream = new MemoryStream();

			if (file.Name.EndsWith(".wav"))
			{
				byte[] data = File.ReadAllBytes(file.FullName);
				memoryStream.Write(data, 0, data.Length);
			}
			else if (file.Name.EndsWith(".ogg"))
			{
				using (var decoder = new Process())
				{
					decoder.StartInfo.FileName = "oggdec.exe";
					decoder.StartInfo.Arguments = "--stdout \"" + file.FullName + "\"";
					decoder.StartInfo.UseShellExecute = false;
					decoder.StartInfo.RedirectStandardOutput = true;
					decoder.StartInfo.CreateNoWindow = true;

					decoder.Start();
					decoder.StandardOutput.BaseStream.CopyTo(memoryStream);
					decoder.WaitForExit();
				}
			}
			else
			{
				throw new ArgumentException("Unsupported audio format");
			}

			memoryStream.Position = 0;
			return memoryStream;
		}
	}
}
