using System;
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
		// private const long Pos_UIVersion      = 0;
		// private const long Pos_UITick         =       sizeof(uint)  + Pos_UIVersion;
		// private const long Pos_AvatarPosition =       sizeof(uint)  + Pos_UITick;
		// private const long Pos_AvatarFront    =   3 * sizeof(float) + Pos_AvatarPosition;
		// private const long Pos_AvatarTop      =   3 * sizeof(float) + Pos_AvatarFront;
		// private const long Pos_Name           =   3 * sizeof(float) + Pos_AvatarTop;
		// private const long Pos_CameraPosition = 256 * sizeof(char)  + Pos_Name;
		// private const long Pos_CameraFront    =   3 * sizeof(float) + Pos_CameraPosition;
		// private const long Pos_CameraTop      =   3 * sizeof(float) + Pos_CameraFront;
		// private const long Pos_Identity       =   3 * sizeof(float) + Pos_CameraTop;
		// private const long Pos_ContextLength  = 256 * sizeof(char)  + Pos_Identity;
		
		private ICoreClientAPI _api;
		private MemoryMappedFile _mappedFile;
		private MemoryMappedViewAccessor _accessor;
		private LinkedMemory _memory;
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			_api = api;
			var structSize = Marshal.SizeOf<LinkedMemory>();
			var mappedFileName = (Environment.OSVersion.Platform == PlatformID.Unix)
				? $"/MumbleLink.{getuid()}" : "MumbleLink";
			_mappedFile = MemoryMappedFile.CreateOrOpen(mappedFileName, structSize);
			_accessor   = _mappedFile.CreateViewAccessor();
			
			api.Event.RegisterGameTickListener(OnGameTick, 20);
		}
		
		private void OnGameTick(float delta)
		{
			if (_api.World?.Player == null) return;
			
			if (_memory.UIVersion != 2) {
				_memory = LinkedMemory.Create(
					"Vintage Story MumbleLink",
					"Enables Vintage Story Mumble positional audio support through the Mumble Link plugin");
				
				// FIXME: Use unique server identifier somehow (IP?).
				_memory.Context  = "";//_api.World.SavegameIdentifier;
				_memory.Identity = _api.World.Player.PlayerUID;
			}
			_memory.UITick++;
			
			var player = _api.World.Player;
			var entity = player.Entity;
			var headPitch = entity.HeadPitch;
			var headYaw   = entity.BodyYaw + entity.HeadYaw;
			
			_memory.AvatarPosition = entity.Pos.XYZ + entity.LocalEyePos;
			_memory.AvatarFront = new Vector3f(
				-GameMath.Sin(headYaw) * GameMath.Cos(headPitch),
				-GameMath.Sin(headPitch),
				 GameMath.Cos(headYaw) * GameMath.Cos(headPitch));
			
			_memory.CameraPosition = entity.CameraPos;
			_memory.CameraFront = new Vector3f(
				-GameMath.Sin(player.CameraYaw) * GameMath.Cos(player.CameraPitch),
				-GameMath.Sin(player.CameraPitch),
				 GameMath.Cos(player.CameraYaw) * GameMath.Cos(player.CameraPitch));
			
			unsafe {
				byte* ptr = null;
				_accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
				Marshal.StructureToPtr(_memory, (IntPtr)ptr, false);
				_accessor.SafeMemoryMappedViewHandle.ReleasePointer();
			}
		}
		
		public override void Dispose()
		{
			_accessor?.Dispose();
			_mappedFile?.Dispose();
		}
		
		[DllImport("libc")]
		private static extern uint getuid();
	}
}