using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

[assembly: ModInfo("MumbleLink",
	Description = "Enables Mumble positional audio support through its Link plugin",
	Website     = "https://github.com/copygirl/MumbleLink",
	Authors     = new []{ "copygirl", "Nikky" },
	Side        = "Client")]

namespace MumbleLink
{
	public class MumbleLinkSystem : ModSystem
	{
		private ICoreClientAPI _api;
		private MumbleLinkData _data = new MumbleLinkData();
		
		// Windows
		private MemoryMappedFile _mappedFile;
		private MemoryMappedViewStream _stream;
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			_api = api;
			
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				var fileName = $"/dev/shm/MumbleLink.{getuid()}";
				_mappedFile = File.Exists(fileName)
					? MemoryMappedFile.CreateFromFile(fileName, FileMode.Open)
					: MemoryMappedFile.CreateFromFile(fileName, FileMode.Create, null, MumbleLinkData.Size);
			} else {
				_mappedFile = MemoryMappedFile.CreateOrOpen("MumbleLink", MumbleLinkData.Size);
			}
			_stream = _mappedFile.CreateViewStream(0, MumbleLinkData.Size);
			
			api.Event.RegisterGameTickListener(OnGameTick, 20);
		}
		
		private void OnGameTick(float delta)
		{
			if ((_api.World?.Player == null) || _api.IsSinglePlayer) return;
			_data.UITick++;
			
			// FIXME: Use unique server identifier somehow (IP?).
			_data.Context  = "";//_api.World.SavegameIdentifier;
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
			_stream?.Dispose();
			_mappedFile?.Dispose();
		}
		
		[DllImport("libc")]
		private static extern uint getuid();
	}
}