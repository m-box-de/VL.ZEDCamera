using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Graphics;

namespace sl
{
    static public class ZEDCamExtensions
    {
        public static void GetTextureParameter(this ZEDMat mat, VIEW view, out int width, out int height, out MipMapCount mitmap, out PixelFormat format, out TextureFlags textureFlags, out int arraySize, out GraphicsResourceUsage usage, out TextureOptions options, out DataPointer dataPointer)
        {
            width =  mat != null ? mat.GetWidth() : 256;
            height = mat != null ? mat.GetHeight() : 256;
            mitmap = 1;
            switch (view)
            {
                case VIEW.LEFT: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.RIGHT: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.LEFT_GREY: format = PixelFormat.A8_UNorm; break;
                case VIEW.RIGHT_GREY: format = PixelFormat.A8_UNorm; break;
                case VIEW.LEFT_UNRECTIFIED: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.RIGHT_UNRECTIFIED: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.LEFT_UNRECTIFIED_GREY: format = PixelFormat.A8_UNorm; break;
                case VIEW.RIGHT_UNRECTIFIED_GREY: format = PixelFormat.A8_UNorm; break;
                case VIEW.SIDE_BY_SIDE: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.DEPTH: format = PixelFormat.R32_Float; break;
                case VIEW.CONFIDENCE: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.NORMALS: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.DEPTH_RIGHT: format = PixelFormat.B8G8R8A8_UNorm; break;
                case VIEW.NORMALS_RIGHT: format = PixelFormat.B8G8R8A8_UNorm; break;
                default: format = PixelFormat.None; break;
            }
            textureFlags = TextureFlags.ShaderResource;
            arraySize = 1;
            usage = GraphicsResourceUsage.Default;
            options = TextureOptions.None;
            dataPointer = mat != null ? new DataPointer(mat.GetPtr(), mat.GetWidth() * mat.GetHeight() * mat.GetPixelBytes()) : new DataPointer();
        }

        public static void GetTextureParameter(this ZEDMat mat, MEASURE measure, out int width, out int height, out MipMapCount mitmap, out PixelFormat format, out TextureFlags textureFlags, out int arraySize, out GraphicsResourceUsage usage, out TextureOptions options, out DataPointer dataPointer)
        {
            width = mat != null ? mat.GetWidth() : 256;
            height = mat != null ? mat.GetHeight() : 256;
            mitmap = 1;

            switch (measure)
            {
                case MEASURE.DISPARITY: format = PixelFormat.R32_Float; break;              //DISPARITY,
                case MEASURE.DEPTH: format = PixelFormat.R32_Float; break;                  //DEPTH,
                case MEASURE.CONFIDENCE: format = PixelFormat.R32_Float; break;             //CONFIDENCE,
                case MEASURE.XYZ: format = PixelFormat.R32G32B32A32_Float; break;           //XYZ,
                case MEASURE.XYZRGBA: format = PixelFormat.R32G32B32A32_Float; break;       //XYZRGBA,
                case MEASURE.XYZBGRA: format = PixelFormat.R32G32B32A32_Float; break;       //XYZBGRA,
                case MEASURE.XYZARGB: format = PixelFormat.R32G32B32A32_Float; break;       //XYZARGB,
                case MEASURE.XYZABGR: format = PixelFormat.R32G32B32A32_Float; break;       //XYZABGR,
                case MEASURE.NORMALS: format = PixelFormat.R32G32B32A32_Float; break;       //NORMALS,
                case MEASURE.DISPARITY_RIGHT: format = PixelFormat.R32_Float; break;        //DISPARITY_RIGHT,
                case MEASURE.DEPTH_RIGHT: format = PixelFormat.R32_Float; break;            //DEPTH_RIGHT,
                case MEASURE.XYZ_RIGHT: format = PixelFormat.R32G32B32A32_Float; break;     //XYZ_RIGHT,
                case MEASURE.XYZRGBA_RIGHT: format = PixelFormat.R32G32B32A32_Float; break; //XYZRGBA_RIGHT,
                case MEASURE.XYZBGRA_RIGHT: format = PixelFormat.R32G32B32A32_Float; break; //XYZBGRA_RIGHT,
                case MEASURE.XYZARGB_RIGHT: format = PixelFormat.R32G32B32A32_Float; break; //XYZARGB_RIGHT,
                case MEASURE.XYZABGR_RIGHT: format = PixelFormat.R32G32B32A32_Float; break; //XYZABGR_RIGHT,
                case MEASURE.NORMALS_RIGHT: format = PixelFormat.R32G32B32A32_Float; break; //NORMALS_RIGHT,
                case MEASURE.DEPTH_U16_MM: format = PixelFormat.R16_UInt; break;            //DEPTH_U16_MM,
                case MEASURE.DEPTH_U16_MM_RIGHT: format = PixelFormat.R16_UInt; break;      //DEPTH_U16_MM_RIGHT
                default: format = PixelFormat.None; ; break;
            }
            textureFlags = TextureFlags.ShaderResource;
            arraySize = 1;
            usage = GraphicsResourceUsage.Default;
            options = TextureOptions.None;
            dataPointer = mat != null ? new DataPointer(mat.GetPtr(), mat.GetWidth() * mat.GetHeight() * mat.GetPixelBytes()) : new DataPointer();
        }
    }
}
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        