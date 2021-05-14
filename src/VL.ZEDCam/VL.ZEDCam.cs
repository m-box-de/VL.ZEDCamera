using sl;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Stride.Graphics;


namespace sl
{
	public class ZEDCam
	{
		ZEDCamera camera;
		InitParameters initParameters;
		object grabLock = new object();

        public GraphicsDevice GraphicsDevice;

		ERROR_CODE lastInitStatus = ERROR_CODE.ERROR_CODE_LAST;
		ERROR_CODE previousInitStatus;


		private bool running;
		public bool Running
		{
			get => running;
		}

		List<ZEDMat> mats;
		int matIndex;

		private RuntimeParameters runtimeParameters;
		public RuntimeParameters RuntimeParameters
		{
			get { return this.runtimeParameters; }
			set { this.runtimeParameters = value; }
		}

        public float FOV_H
        {
            get { return this.camera.HorizontalFieldOfView; }
            //get { return this.camera.CalibrationParametersRectified.leftCam.hFOV; }
        }

        public float FOV_V
        {
            get { return this.camera.VerticalFieldOfView; }
            //get { return this.camera.CalibrationParametersRectified.leftCam.vFOV; }
        }

        public int FPS
        {
            get { return (int)this.camera.GetCameraFPS(); }
            set { this.camera.SetCameraFPS(value); }
        }

        public void setFPS (fps x)
        {
            int t = 30;
            switch(x)
            {
                case fps._15FPS: t = 15; break;
                case fps._30FPS: t = 30; break;
                case fps._60FPS: t = 60; break;
                case fps._100FPS:t = 100; break;
            }
            FPS = t;
        }

        // constructor
        public ZEDCam() : this(null, null) { }

		public ZEDCam(string svoPath) : this(null, svoPath) { }

		public ZEDCam(InitParameters parameters = null, string svoPath = null)
		{
			camera = new ZEDCamera();
            
            mats = new List<ZEDMat>();

			if (parameters == null)
			{
				parameters = new InitParameters();
				parameters.resolution = RESOLUTION.HD720;
				parameters.depthMode = DEPTH_MODE.QUALITY;
				parameters.depthStabilization = true;
				parameters.enableRightSideMeasure = true; // isStereoRig;

				parameters.coordinateUnit = UNIT.MILLIMETER;
				parameters.depthMinimumDistance = 200f;
            }

			if (svoPath != null)
			{
				parameters.pathSVO = svoPath;
			}

			this.initParameters = parameters;

			// runtime parameters
			runtimeParameters = new RuntimeParameters()
			{
				sensingMode = SENSING_MODE.FILL,
				enableDepth = true
			};

			// create the camera
			camera.CreateCamera(0,true);
		}

		public ZEDCam(RESOLUTION resolution, DEPTH_MODE depthMode = DEPTH_MODE.QUALITY, bool stabilisation = true)
			: this(new InitParameters
			{
				resolution = resolution,
				depthMode = depthMode,
				depthStabilization = stabilisation,
				enableRightSideMeasure = true,

				coordinateUnit = UNIT.MILLIMETER,
				depthMinimumDistance = 200f
			})
		{ }

        private Texture _texture;

        public Texture FetchImage(GraphicsDevice device, GraphicsContext context, VIEW view)
        {
            if (mats.Count < matIndex + 1)
            {
                mats.Add(new ZEDMat());
            }
            var mat = mats[matIndex++];
            camera.RetrieveImage(mat, view);

            /*           
                                   MatType cvType;

                                   switch (view)
                                   {
                                       case VIEW.LEFT: cvType = MatType.CV_8UC4; break;
                                       case VIEW.RIGHT: cvType = MatType.CV_8UC4; break;
                                       case VIEW.LEFT_GREY: cvType = MatType.CV_8UC1; break;
                                       case VIEW.RIGHT_GREY: cvType = MatType.CV_8UC1; break;
                                       case VIEW.LEFT_UNRECTIFIED: cvType = MatType.CV_8UC4; break;
                                       case VIEW.RIGHT_UNRECTIFIED: cvType = MatType.CV_8UC4; break;
                                       case VIEW.LEFT_UNRECTIFIED_GREY: cvType = MatType.CV_8UC1; break;
                                       case VIEW.RIGHT_UNRECTIFIED_GREY: cvType = MatType.CV_8UC1; break;
                                       case VIEW.SIDE_BY_SIDE: cvType = MatType.CV_8UC4; break;
                                       case VIEW.DEPTH: cvType = MatType.CV_32FC1; break;
                                       case VIEW.CONFIDENCE: cvType = MatType.CV_8UC4; break;
                                       case VIEW.NORMALS: cvType = MatType.CV_8UC4; break;
                                       case VIEW.DEPTH_RIGHT: cvType = MatType.CV_8UC4; break;
                                       case VIEW.NORMALS_RIGHT: cvType = MatType.CV_8UC4; break;
                                       default: cvType = -1; break;
                                   }
                                   return new Mat(camera.ImageHeight, view == VIEW.SIDE_BY_SIDE ? camera.ImageWidth * 2 : camera.ImageWidth, cvType, mat.GetPtr());
                        */
            MipMapCount mitmap = 1;
            PixelFormat imgType;

            switch (view)
            {
                case VIEW.LEFT:                     imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.RIGHT:                    imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.LEFT_GREY:                imgType = PixelFormat.A8_UNorm; break;
                case VIEW.RIGHT_GREY:               imgType = PixelFormat.A8_UNorm; break;
                case VIEW.LEFT_UNRECTIFIED:         imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.RIGHT_UNRECTIFIED:        imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.LEFT_UNRECTIFIED_GREY:    imgType = PixelFormat.A8_UNorm; break;
                case VIEW.RIGHT_UNRECTIFIED_GREY:   imgType = PixelFormat.A8_UNorm; break;
                case VIEW.SIDE_BY_SIDE:             imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.DEPTH:                    imgType = PixelFormat.R32_Float; break;
                case VIEW.CONFIDENCE:               imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.NORMALS:                  imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.DEPTH_RIGHT:              imgType = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.NORMALS_RIGHT:            imgType = PixelFormat.B8G8R8A8_UNorm; break;
                default:                            imgType = PixelFormat.None; break;
            }

            if (_texture == null && device != null)
                _texture = Texture.New2D(device, 
                    view == VIEW.SIDE_BY_SIDE ? camera.ImageWidth * 2 : camera.ImageWidth, 
                    camera.ImageHeight, 
                    imgType,
                    TextureFlags.ShaderResource,
                    1,
                    GraphicsResourceUsage.Default,
                    TextureOptions.None);

            Stride.Graphics.DataPointer dp = new DataPointer(mat.GetPtr(), mat.GetWidth() * mat.GetHeight() * mat.GetPixelBytes());
            this._texture.SetData(context.CommandList, dp);
            return _texture;
            // TextureFlags.ShaderResource,  mitmap, imgType, 1, mat.GetPtr());
        }

        private Texture _depth;

        public Texture FetchPointCloud(GraphicsDevice device, GraphicsContext context, MEASURE measure)
        {
            if (mats.Count < matIndex + 1)
            {
                mats.Add(new ZEDMat());
            }
            var depth_zed = mats[matIndex++];
            camera.RetrieveMeasure(depth_zed, measure);

            /*
			MatType cvType;

			switch (measure)
			{
				case MEASURE.DISPARITY:			cvType = MatType.CV_32FC1; break;
				case MEASURE.DEPTH:				cvType = MatType.CV_32FC1; break;
				case MEASURE.CONFIDENCE:		cvType = MatType.CV_32FC1; break;
				case MEASURE.XYZ:				cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZRGBA:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZBGRA:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZARGB:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZABGR:			cvType = MatType.CV_32FC4; break;
				case MEASURE.NORMALS:			cvType = MatType.CV_32FC4; break;
				case MEASURE.DISPARITY_RIGHT:	cvType = MatType.CV_32FC1; break;
				case MEASURE.DEPTH_RIGHT:		cvType = MatType.CV_32FC1; break;
				case MEASURE.XYZ_RIGHT:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZRGBA_RIGHT:		cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZBGRA_RIGHT:		cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZARGB_RIGHT:		cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZABGR_RIGHT:		cvType = MatType.CV_32FC4; break;
				default:						cvType = -1; break;
			}

			return new Mat(camera.ImageHeight, camera.ImageWidth, cvType, depth_zed.GetPtr());
            */
            MipMapCount mitmap = 1;
            PixelFormat imgType;

            switch (measure)
            {
                case MEASURE.DISPARITY:         imgType = PixelFormat.R32_Float; break;
                case MEASURE.DEPTH:             imgType = PixelFormat.R32_Float; break;
                case MEASURE.CONFIDENCE:        imgType = PixelFormat.R32_Float; break;
                case MEASURE.XYZ:               imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZRGBA:           imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZBGRA:           imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZARGB:           imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZABGR:           imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.NORMALS:           imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.DISPARITY_RIGHT:   imgType = PixelFormat.R32_Float; break;
                case MEASURE.DEPTH_RIGHT:       imgType = PixelFormat.R32_Float; break;
                case MEASURE.XYZ_RIGHT:         imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZRGBA_RIGHT:     imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZBGRA_RIGHT:     imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZARGB_RIGHT:     imgType = PixelFormat.R32G32B32A32_Float; break;
                case MEASURE.XYZABGR_RIGHT:     imgType = PixelFormat.R32G32B32A32_Float; break;
                default:                        imgType = PixelFormat.None; ; break;
            }

            if (_depth == null && device != null)
                _depth = Texture.New2D(device,
                    camera.ImageWidth,
                    camera.ImageHeight,
                    imgType,
                    TextureFlags.ShaderResource,
                    1,
                    GraphicsResourceUsage.Default,
                    TextureOptions.None);

            var w = depth_zed.GetWidth();
            var h = depth_zed.GetHeight();
            var p = depth_zed.GetPixelBytes();
            var pointer = depth_zed.GetPtr();

            Stride.Graphics.DataPointer _dp = new DataPointer(pointer, w * h * p );
            this._depth.SetData(context.CommandList, _dp);

            return _depth;

            //return Image.New2D(camera.ImageWidth, camera.ImageHeight, mitmap, imgType, 1, depth_zed.GetPtr());
        }

        public void InitZED_Sequential()
		{
			while (lastInitStatus != sl.ERROR_CODE.SUCCESS)
			{
				lastInitStatus = camera.Init(ref initParameters);
                if (lastInitStatus != sl.ERROR_CODE.SUCCESS)
				{
					lastInitStatus = camera.Init(ref initParameters);
					previousInitStatus = lastInitStatus;
				}
				Thread.Sleep(300);
			}
        }

		public void Start_Sequential()
		{
			if (running == true) return;
			InitZED_Sequential();
			running = true;
		}

		public void GrabOnce()
		{
			if (running)
			{
				lock (grabLock)
				{
					sl.ERROR_CODE e = camera.Grab(ref runtimeParameters);
					// reset mat index
					matIndex = 0;
				}
			}
			else
			{
				//Console.WriteLine("Not open");
			}
		}

		public void Stop()
		{
			this.running = false;
		}

		public void Close()
		{
            camera.Destroy();
		}

		public void TT()
		{
			camera.SetCameraSettings();
		}

        public enum fps
        {
            /// <summary>
            /// 2208*1242. Supported frame rate: 15 FPS.
            /// </summary>
            _15FPS,
            /// <summary>
            /// 1920*1080. Supported frame rates: 15, 30 FPS.
            /// </summary>
            _30FPS,
            /// <summary>
            /// 1280*720. Supported frame rates: 15, 30, 60 FPS.
            /// </summary>
            _60FPS,
            /// <summary>
            /// 672*376. Supported frame rates: 15, 30, 60, 100 FPS.
            /// </summary>
            _100FPS
        };
    }
}
