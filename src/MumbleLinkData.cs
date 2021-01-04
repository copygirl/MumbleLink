using System;
using System.IO;
using System.Text;
using Vintagestory.API.MathTools;

namespace MumbleLink
{
	public class MumbleLinkData
	{
		public static int Size { get; } = (Environment.OSVersion.Platform == PlatformID.Unix) ? 10580 : 5460;
		
		public uint UIVersion { get; } = 2;
		public uint UITick { get; set; }
		
		/// <summary> The display name of this link. Will be shown in the Mumble chat log when linked. </summary>
		public string Name { get; set; } = "Vintage Story";
		/// <summary> The description of this link. </summary>
		public string Description { get; set; } = "Enables Vintage Story Mumble positional audio support through the Mumble Link plugin";
		
		/// <summary> A string uniquely identifying the user. </summary>
		public string Identity { get; set; }
		/// <summary> A string uniquely identifying the context the user is in, for
		///           example which server / game / map they're connected to. Users will
		///           only hear each other positionally if they share the same context. </summary>
		public string Context { get; set; }
		
		public Vec3d AvatarPosition { get; set; } = Vec3d.Zero;
		public Vec3d AvatarFront { get; set; } = Vec3d.Zero;
		public Vec3d AvatarTop { get; set; } = Vec3d.Zero;
		
		public Vec3d CameraPosition { get; set; } = Vec3d.Zero;
		public Vec3d CameraFront { get; set; } = Vec3d.Zero;
		public Vec3d CameraTop { get; set; } = Vec3d.Zero;
		
		public void Write(Stream stream)
		{
			var encoding = (Environment.OSVersion.Platform == PlatformID.Unix) ? Encoding.UTF32 : Encoding.Unicode;
			using (var writer = new BinaryWriter(stream, encoding, true)) {
				writer.Write(UIVersion);
				writer.Write(UITick);
				WriteVec3d(writer, AvatarPosition);
				WriteVec3d(writer, AvatarFront);
				WriteVec3d(writer, AvatarTop);
				WriteString(writer, 256, Name);
				WriteVec3d(writer, CameraPosition);
				WriteVec3d(writer, CameraFront);
				WriteVec3d(writer, CameraTop);
				WriteString(writer, 256, Identity);
				WriteString(writer, 256, Context, Encoding.UTF8, true);
				WriteString(writer, 2048, Description);
			}
		}
		
		private void WriteVec3d(BinaryWriter writer, Vec3d vector)
		{
			writer.Write((float)vector.X);
			writer.Write((float)vector.Y);
			writer.Write((float)vector.Z);
		}
		
		private void WriteString(BinaryWriter writer, int size, string value, Encoding encoding = null, bool lengthPrefixed = false)
		{
			encoding = encoding ?? ((Environment.OSVersion.Platform == PlatformID.Unix) ? Encoding.UTF32 : Encoding.Unicode);
			var bytes  = new byte[encoding.GetByteCount(" ") * size];
			var length = encoding.GetBytes(value, 0, value.Length, bytes, 0);
			if (lengthPrefixed) writer.Write((uint)length);
			writer.Write(bytes);
		}
	}
}
