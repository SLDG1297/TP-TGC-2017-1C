//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

//variables
float time;
float rotationTime;
float amplitud_vaiven;

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

//Input del Vertex Shader
struct VS_INPUT
{
	float4 Position : POSITION0; //posicion0: posicion del vertice
	float4 Color : COLOR0;
	float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT
{
	float4 Position :        POSITION0;
	float2 Texcoord :        TEXCOORD0;
	float4 Color :			COLOR0;
};

//Vertex Shader
VS_OUTPUT vs_main(VS_INPUT Input)
{
	VS_OUTPUT Output;
	//Proyectar posicion
	Output.Position = mul(Input.Position, matWorldViewProj);

	//Propago las coordenadas de textura
	Output.Texcoord = Input.Texcoord;

	//Propago el color x vertice
	Output.Color = Input.Color;

	return(Output);
}

//Pixel Shader
float4 ps_main(float3 Texcoord: TEXCOORD0, float3 N : TEXCOORD1,
	float3 Pos : TEXCOORD2) : COLOR0
{
	//Obtener el texel de textura
	float4 fvBaseColor = tex2D(diffuseMap, Texcoord);
	return (fvBaseColor);
}

technique DefaultTechnique
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 vs_main();
		PixelShader = compile ps_2_0 ps_main();
	}
}

VS_OUTPUT vs_vaiven_circulo_xz(VS_INPUT Input)
{
	VS_OUTPUT Output;

	float X = Input.Position.x;
	float Z = Input.Position.z;
	Input.Position.x += 1000* sin(rotationTime);
	Input.Position.z += 1000 * cos(rotationTime);

	//Proyectar posicion
	Output.Position = mul(Input.Position, matWorldViewProj);

	//Propago las coordenadas de textura
	Output.Texcoord = Input.Texcoord;

	// Animar color
	Input.Color.r = abs(sin(rotationTime));
	Input.Color.g = abs(cos(rotationTime));

	//Propago el color x vertice
	Output.Color = Input.Color;

	return(Output);
}

VS_OUTPUT vs_ondulacion_z(VS_INPUT Input)
{
	VS_OUTPUT Output;

	Input.Position.z += amplitud_vaiven *sin(time);

	//Proyectar posicion
	Output.Position = mul(Input.Position, matWorldViewProj);

	//Propago las coordenadas de textura
	Output.Texcoord = Input.Texcoord;

	// Animar color
	Input.Color.r = abs(sin(time));
	Input.Color.g = abs(cos(time));

	//Propago el color x vertice
	Output.Color = Input.Color;

	return(Output);
}


technique CirculoXZ
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 vs_vaiven_circulo_xz();
		PixelShader = compile ps_2_0 ps_main();
	}
}

technique OndulacionZ
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 vs_ondulacion_z();
		PixelShader = compile ps_2_0 ps_main();
	}
}



