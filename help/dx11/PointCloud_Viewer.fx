//@author: kopffarben, rmt
//@help: PointCloudViewer for ZEDCam		
//@tags: XYZC
//@credits: 
static const float pi2 = 6.283185307;


Texture2D texture2d <string uiname="Texture";>;
SamplerState Sampler : IMMUTABLE
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};
 
cbuffer cbPerDraw : register( b0 )
{
	float  Alpha = 1.0;
	float  Size = 1.0;
	
	float4x4 tVP : VIEWPROJECTION;
	float4x4 tWVP: WORLDVIEWPROJECTION;
	float4x4 tIV : VIEWINVERSE;
};

float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;

};


struct vsIn
{
	uint vid : SV_InstanceID;
};

struct gsIn_color
{
    float4 pos		: POSITION ;
	float4 col		: COLOR ;
};

struct gsIn
{
    float4 pos		: POSITION ;
};

struct psIn_color
{
    float4 pos		: SV_Position ;
	float2 uv		: TEXCOORD ;
	float4 col		: COLOR ;
 };

struct psIn
{
    float4 pos		: SV_Position ;
	float2 uv		: TEXCOORD ;
};

//Quad position/uvs
float3 g_positions[4]: IMMUTABLE =
{
float3( -1, 1, 0 ),
float3( 1, 1, 0 ),
float3( -1, -1, 0 ),
float3( 1, -1, 0 ),
};

float2 g_texcoords[4]: IMMUTABLE = 
{ 
float2(0,1), 
float2(1,1),
float2(0,0),
float2(1,0),
};


gsIn_color VS_c(vsIn input)
{
    gsIn_color output;
	
	uint width,height;
	texture2d.GetDimensions( width, height);
	
	int x = input.vid % width;
	int y;
	modf((float)input.vid/(float)width,y);

	float4 xyzc = texture2d.Load(int3(x,y,0));
	
	uint  c = asuint(xyzc.w);
	float r = ((c & 0x000000FF )       )/256.0f ;
	float g = ((c & 0x0000FF00 ) >> 8  )/256.0f ;
	float b = ((c & 0x00FF0000 ) >> 16 )/256.0f ;
	float a = ((c & 0xFF000000 ) >> 24 )/256.0f ;	
	
	output.pos     = mul(float4(xyzc.xyz,1), tW);
	output.col     = float4(r,g,b,a);
	
	return output;
}

gsIn VS(vsIn input)
{
    gsIn output;
	
	uint width,height;
	texture2d.GetDimensions( width, height);
	
	int x = input.vid % width;
	int y;
	modf((float)input.vid/(float)width,y);

	float3 xyz = texture2d.Load(int3(x,y,0)).xyz;	
	
	output.pos = mul(float4(xyz,1), tW);
	
	return output;
}

[maxvertexcount(4)]
void GS_QUAD_c(point gsIn_color input[1], inout TriangleStream<psIn_color> SpriteStream)
{
    psIn_color output;
	output.col = input[0].col;

	if (Alpha > 0)
	{
		// Per Vertex Out	
	    for(int i=0; i<4; i++)
	    {
	    	// Get Position with Size
	    	output.pos.xyz = g_positions[i]*Size;
	    	
	    	output.pos.w =1;
	    	
	    	//Make sure quad will face camera
	        output.pos.xyz = mul( output.pos.xyz, (float3x3)tIV ).xyz + input[0].pos.xyz;
	
	    	//Project vertex
	    	output.pos = mul( float4(output.pos.xyz,1.0), tVP );
	    	
	    	// Fill Color and UV
	    	output.uv  = g_texcoords[i];
	    	
	    	SpriteStream.Append(output);
	    }
		SpriteStream.RestartStrip();	
	}
}

[maxvertexcount(4)]
void GS_QUAD(point gsIn input[1], inout TriangleStream<psIn> SpriteStream)
{
    psIn output;

	if (Alpha > 0)
	{
		// Per Vertex Out	
	    for(int i=0; i<4; i++)
	    {
	    	// Get Position with Size
	    	output.pos.xyz = g_positions[i]*Size;
	    	
	    	output.pos.w =1;
	    	
	    	//Make sure quad will face camera
	        output.pos.xyz = mul( output.pos.xyz, (float3x3)tIV ).xyz + input[0].pos.xyz;
	
	    	//Project vertex
	    	output.pos = mul( float4(output.pos.xyz,1.0), tVP );
	    	
	    	// Fill Color and UV
	    	output.uv  = g_texcoords[i];
	    	
	    	SpriteStream.Append(output);
	    }
		SpriteStream.RestartStrip();	
	}
}


float4 PS_HALO(psIn_color input) : SV_Target
{   
 	// CALC HALO HERE
	float Gamma = 0.5;
	float g=Gamma/(1.00001-Gamma);
	float f=length(input.uv-.5);
	f=saturate(1-2*f);
	f=smoothstep(0,1,f);
	f=pow(f,g);

	float4 output = float4(input.col.xyz,f * Alpha);
	
	return output;
}

float4 PS_SOLID(psIn input) : SV_Target
{   
	return cAmb;
}

technique10 RenderParticlesHalo
{
    pass p0
    {
        SetVertexShader( CompileShader( vs_5_0, VS_c() ) );
        SetGeometryShader( CompileShader( gs_5_0, GS_QUAD_c() ) );
        SetPixelShader( CompileShader( ps_5_0, PS_HALO() ) );     
    }  
}
technique10 RenderParticlesSolid
{
    pass p0
    {
        SetVertexShader( CompileShader( vs_5_0, VS() ) );
        SetGeometryShader( CompileShader( gs_5_0, GS_QUAD() ) );
        SetPixelShader( CompileShader( ps_5_0, PS_SOLID() ) );     
    }  
}
