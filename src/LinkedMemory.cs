using System;
using System.Runtime.InteropServices;
using System.Text;
using Vintagestory.API.MathTools;

namespace MumbleLink
{
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	public struct LinkedMemory
	{
		public uint UIVersion { get; private set; }
		public uint UITick { get; set; }
		
		public Vector3f AvatarPosition { get; set; }
		public Vector3f AvatarFront { get; set; }
		public Vector3f AvatarTop { get; set; }
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private char[] _name;
		
		public Vector3f CameraPosition { get; set; }
		public Vector3f CameraFront { get; set; }
		public Vector3f CameraTop { get; set; }
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private char[] _identity;
		
		private uint _contextLength;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] _context;
		
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2048)]
		private char[] _description;
		
		
		public string Identity {
			get => new string(_identity);
			set {
				value.CopyTo(0, _identity, 0, value.Length);
				_identity[value.Length] = '\0';
			}
		}
		
		public string Context {
			get => Encoding.ASCII.GetString(_context, 0, (int)_contextLength);
			set => _contextLength = (uint)Encoding.ASCII.GetBytes(value, 0, value.Length, _context, 0);
		}
		
		
		public static LinkedMemory Create(string name, string description)
		{
			var mem = new LinkedMemory();
			mem.UIVersion = 2;
			
			mem._name        = new char[256];
			mem._identity    = new char[256];
			mem._context     = new byte[256];
			mem._description = new char[2048];
			
			name.CopyTo(0, mem._name, 0, name.Length);
			description.CopyTo(0, mem._description, 0, description.Length);
			
			return mem;
		}
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vector3f
	{
		public float X, Y, Z;
		public Vector3f(float x, float y, float z)
			{ X = x; Y = y; Z = z; }
		
		public static implicit operator Vector3f(Vec3d vec)
			=> new Vector3f((float)vec.X, (float)vec.Y, (float)vec.Z);
		
		public override string ToString()
			=> $"[{X},{Y},{Z}]";
	}
}
