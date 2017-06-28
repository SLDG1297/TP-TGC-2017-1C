/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

#define SIDE_TO_SIDE_FREQ1 1.975
#define SIDE_TO_SIDE_FREQ2 0.793
#define UP_AND_DOWN_FREQ1 0.375
#define UP_AND_DOWN_FREQ2 0.193

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

float4 SmoothCurve( float4 x ) {  
  return x * x *( 3.0 - 2.0 * x );  
}

float4 TriangleWave( float4 x ) {  
  return abs( frac( x + 0.5 ) * 2.0 - 1.0 );  
}

float4 SmoothTriangleWave( float4 x ) {  
  return SmoothCurve( TriangleWave( x ) );  
} 

float time;

/**************************************************************************************/
/* RenderScene */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT
{
	float4 Position : POSITION0; //POSITION0: posicion del vertice
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0; //primera coordenada de texturas uv
	float3 Normal :   NORMAL0;
};

//Output del Vertex Shader
struct VS_OUTPUT
{
	float4 Position :        POSITION0;
	float2 TexCoord :        TEXCOORD0;
	float4 Color :			COLOR0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
};

void ApplyDetailBending(
	inout float3 vPos,		// The final world position of the vertex being modified
	float3 vNormal,			// The world normal for this vertex
	float3 objectPosition,	// The world position of the plant instance (same for all vertices)
	float fDetailPhase,		// Optional phase for side-to-side. This is used to vary the phase for side-to-side motion
	float fBranchPhase,		// The green vertex channel per Crytek's convention
	float fTime,			// Ever-increasing time value (e.g. seconds ellapsed)
	float fEdgeAtten,		// "Leaf stiffness", red vertex channel per Crytek's convention
	float fBranchAtten,		// "Overall stiffness", *inverse* of blue channel per Crytek's convention
	float fBranchAmp,		// Controls how much up and down
	float fSpeed,			// Controls how quickly the leaf oscillates
	float fDetailFreq,		// Same thing as fSpeed (they could really be combined, but I suspect
							// this could be used to let you additionally control the speed per vertex).
	float fDetailAmp)		// Controls how much back and forth
{
	// Phases (object, vertex, branch)
	// fObjPhase: This ensures phase is different for different plant instances, but it should be
	// the same value for all vertices of the same plant.
	float fObjPhase = dot(objectPosition.xyz, 1);  

	// In this sample fBranchPhase is always zero, but if you want you could somehow supply a
	// different phase for each branch.
	fBranchPhase += fObjPhase;

	// Detail phase is (in this sample) controlled by the GREEN vertex color. In your modelling program,
	// assign the same "random" phase color to each vertex in a single leaf/branch so that the whole leaf/branch
	// moves together.
	float fVtxPhase = dot(vPos.xyz, fDetailPhase + fBranchPhase);  

	float2 vWavesIn = fTime + float2(fVtxPhase, fBranchPhase );
	float4 vWaves = (frac( vWavesIn.xxyy *
					   float4(SIDE_TO_SIDE_FREQ1, SIDE_TO_SIDE_FREQ2, UP_AND_DOWN_FREQ1, UP_AND_DOWN_FREQ2) ) *
					   2.0 - 1.0 ) * fSpeed * fDetailFreq;
	vWaves = SmoothTriangleWave( vWaves );
	float2 vWavesSum = vWaves.xz + vWaves.yw;  

	// -fBranchAtten is how restricted this vertex of the leaf/branch is. e.g. close to the stem
	//  it should be 0 (maximum stiffness). At the far outer edge it might be 1.
	//  In this sample, this is controlled by the blue vertex color.
	// -fEdgeAtten controls movement in the plane of the leaf/branch. It is controlled by the
	//  red vertex color in this sample. It is supposed to represent "leaf stiffness". Generally, it
	//  should be 0 in the middle of the leaf (maximum stiffness), and 1 on the outer edges.
	// -Note that this is different from the Crytek code, in that we use vPos.xzy instead of vPos.xyz,
	//  because I treat y as the up-and-down direction.

        vPos.xyz += vWavesSum.x * float3(fEdgeAtten * fDetailAmp * vNormal.xyz);
        vPos.y += vWavesSum.y * fBranchAtten * fBranchAmp;
}

void MainBending(){
	//multiplicar vector viento por velocidad
	//a eso, usar una funcion seno
	
	
}


// This bends the entire plant in the direction of the wind.
// vPos:		The world position of the plant *relative* to the base of the plant.
//			(That means we assume the base is at (0, 0, 0). Ensure this before calling this function).
// vWind:		The current direction and strength of the wind.
// fBendScale:	How much this plant is affected by the wind.
void ApplyMainBending(inout float3 vPos, float2 vWind, float fBendScale)
{
	// Calculate the length from the ground, since we'll need it.
	float fLength = length(vPos);
	// Bend factor - Wind variation is done on the CPU.
	float fBF = vPos.y * fBendScale;
	// Smooth bending factor and increase its nearby height limit.
	fBF += 1.0;
	fBF *= fBF;
	fBF = fBF * fBF - fBF;
	// Displace position
	float3 vNewPos = vPos;
	vNewPos.xz += vWind.xy * fBF;
	// Rescale - this keeps the plant parts from "stretching" by shortening the y (height) while
	// they move about the xz.
	vPos.xyz = normalize(vNewPos.xyz)* fLength;
}

float BendScale = 0.5;
float BranchAmplitude = 0.2;
float DetailAmplitude = 0.4;
float3 WindSpeed = float3(0.1,0.3,0);

VS_OUTPUT vs_viento(VS_INPUT Input){
	VS_OUTPUT Output;
		
	// Grab the object position from the translation part of the world matrix.
	// We need the object position because we want to temporarily translate the vertex
	// back to the position it would be if the plant were at (0, 0, 0).
	// This is necessary for the main bending to work.	
	float3 objectPosition = float3(matWorld._m30, matWorld._m31, matWorld._m32);
	
	//vPos -= objectPosition;	// Reset the vertex to base-zero
	//ApplyMainBending(vPos, WindSpeed, BendScale);
	//vPos += objectPosition;	// Restore it.	
	
	Output.WorldNormal = normalize(mul(Input.Normal, matWorld));
	
	float windStrength = length(WindSpeed);
	
	ApplyDetailBending(
		Input.Position.xyz,
		Output.WorldNormal,
		objectPosition,
		0,					// Leaf phase - not used in this scenario, but would allow for variation in side-to-side motion
		Input.Color.g,		// Branch phase - should be the same for all verts in a leaf/branch.
		time,
		Input.Color.r,		// edge attenuation, leaf stiffness
		1 - Input.Color.b,  // branch attenuation. High values close to stem, low values furthest from stem.
							// For some reason, Crysis uses solid blue for non-moving, and black for most movement.
							// So we invert the blue value here.
		BranchAmplitude * windStrength, // branch amplitude. Play with this until it looks good.
		1,					// Speed. Play with this until it looks good.
		1,					// Detail frequency. Keep this at 1 unless you want to have different per-leaf frequency
		DetailAmplitude * windStrength	// Detail amplitude. Play with this until it looks good.
		);
	//float4 viewPosition = mul(float4(vPos, worldPosition.w), matWorldView);
	
	float4 worldPosition = mul(Input.Position, matWorld);
	float3 vPos = worldPosition.xyz;
	
	Output.WorldPosition = vPos.xyz;
	Output.Position = mul(Input.Position, matWorldViewProj);
	
	Output.TexCoord = Input.TexCoord;
	Output.Color = Input.Color;		
	return Output;
}


//Vertex Shader
VS_OUTPUT vs_main(VS_INPUT Input)
{
	VS_OUTPUT Output;

	//Proyectar posicion
	Output.Position = mul(Input.Position, matWorldViewProj);


	// Calculo la posicion real
	float4 pos_real = mul(Input.Position, matWorld);
	Output.WorldPosition = float3(pos_real.x, pos_real.y, pos_real.z);

	// Transformo la normal y la normalizo
	//Output.Norm = normalize(mul(Input.Normal,matInverseTransposeWorld));
	Output.WorldNormal = normalize(mul(Input.Normal, matWorld));
	
		//Las Texcoord quedan igual
	Output.TexCoord = Input.TexCoord;
	Output.Color = Input.Color;	
	return(Output);
	
}

//Pixel Shader
float4 ps_main(float2 Texcoord: TEXCOORD0, float4 Color : COLOR0) : COLOR0
{
	// Obtener el texel de textura
	// diffuseMap es el sampler, Texcoord son las coordenadas interpoladas
	float4 fvBaseColor = tex2D(diffuseMap, Texcoord);
	// combino color y textura
	// en este ejemplo scombino un 80% el color de la textura y un 20%el del vertice
	return fvBaseColor;
}

technique Wind
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 vs_viento();
		PixelShader = compile ps_2_0 ps_main();
	}
}

technique DefaultTechnique
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 vs_main();
		PixelShader = compile ps_2_0 ps_main();
	}
}















