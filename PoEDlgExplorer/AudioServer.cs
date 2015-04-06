using System;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace PoEDlgExplorer
{
	public static class AudioServer
	{
		private static SoundPlayer _player = new SoundPlayer();

		public static void Play(FileInfo file)
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
			_player.Stream = memoryStream;
			_player.Play();
		}

		public static void Stop()
		{
			_player.Stop();
		}
	}
}
