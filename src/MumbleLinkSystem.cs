using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

[assembly: ModInfo("MumbleLink",
	Description = "Enables Mumble positional audio support through its Link plugin",
	Website = "https://github.com/copygirl/MumbleLink",
	Authors = new []{ "copygirl", "Nikky" },
	Version = "1.1.0", Side = "Client")]

namespace MumbleLink
{
	public class MumbleLinkSystem : ModSystem, IDisposable
	{
		private ICoreClientAPI _api;
		private MemoryMappedFile _mappedFile;
		private MemoryMappedViewStream _stream;
		private FileSystemWatcher _watcher;
		
		private MumbleLinkData _data = new MumbleLinkData();
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			_api = api;
			api.Event.RegisterGameTickListener(OnGameTick, 20);
			
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				var fileName = $"/dev/shm/MumbleLink.{getuid()}";
				
				void OnCreated(object sender, FileSystemEventArgs e)
				{
					Mod.Logger.Notification("Link established");
					_mappedFile = MemoryMappedFile.CreateFromFile(fileName);
					_stream     = _mappedFile.CreateViewStream(0, MumbleLinkData.Size);
				}
				void OnDeleted(object sender, FileSystemEventArgs e)
				{
					Mod.Logger.Notification("Link lost");
					_stream.Dispose();
					_mappedFile.Dispose();
					_stream     = null;
					_mappedFile = null;
				}
				
				if (File.Exists(fileName))
					OnCreated(null, null);
				
				_watcher = new FileSystemWatcher(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
				_watcher.Created += OnCreated;
				_watcher.Deleted += OnDeleted;
				_watcher.EnableRaisingEvents = true;
			} else {
				_mappedFile = MemoryMappedFile.CreateOrOpen("MumbleLink", MumbleLinkData.Size);
				_stream     = _mappedFile.CreateViewStream(0, MumbleLinkData.Size);
			}
		}
		
		private void OnGameTick(float delta)
		{
			if ((_stream == null) || (_api.World?.Player == null) || _api.IsSinglePlayer) return;
			_data.UITick++;
			
			_data.Context  = _api.World.Seed.ToString();
			_data.Identity = _api.World.Player.PlayerUID;
			
			var player = _api.World.Player;
			var entity = player.Entity;
			
			var headPitch = entity.HeadPitch;
			var headYaw   = entity.BodyYaw + entity.HeadYaw;
			_data.AvatarPosition = entity.Pos.XYZ + entity.LocalEyePos;
			_data.AvatarFront = new Vec3d(
				-GameMath.Cos(headYaw) * GameMath.Cos(headPitch),
				-GameMath.Sin(headPitch),
				 GameMath.Sin(headYaw) * GameMath.Cos(headPitch));
			
			_data.CameraPosition = entity.CameraPos;
			_data.CameraFront = new Vec3d(
				-GameMath.Cos(player.CameraYaw) * -GameMath.Cos(player.CameraPitch),
				 GameMath.Sin(player.CameraPitch),
				 GameMath.Sin(player.CameraYaw) * -GameMath.Cos(player.CameraPitch));
			
			_stream.Position = 0;
			_data.Write(_stream);
		}
		
		public override void Dispose()
		{
			_watcher?.Dispose();
			_stream?.Dispose();
			_mappedFile?.Dispose();
		}
		
		[DllImport("libc")]
		private static extern uint getuid();
	}
}
