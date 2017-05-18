using System.Drawing;
using Microsoft.DirectX;
using TGC.Core.Camara;
using TGC.Core.Input;
using TGC.Core.Utils;

namespace TGC.Group.Model.Cameras
{
    public class MenuCamera : TgcCamera
    {
        public MenuCamera(Size windowSize)
        {
			CameraCenter = new Vector3(-windowSize.Width / 4, -windowSize.Height / 4, 0);
            NextPos = new Vector3(CameraCenter.X, CameraCenter.Y, CameraDistance);
			CameraDistance = windowSize.Height / 2;
			UpVector = DEFAULT_UP_VECTOR;
            base.SetCamera(NextPos, LookAt, UpVector);
        }

        public override void UpdateCamera(float elapsedTime)
        {
			NextPos = new Vector3(CameraCenter.X, CameraCenter.Y, CameraDistance);
			base.SetCamera(NextPos, CameraCenter, UpVector);
        }

        public Vector3 CameraCenter { get; set; }

        public float CameraDistance { get; set; }

        public Vector3 NextPos { get; set; }

    }
}