shader_type particles;

uniform float SnowSpreadWidth: hint_range(0.0, 1.0);
uniform float SnowMobileVelocity = 1.0;
uniform sampler2D CollorPallete;


float rand(uint seed) {
    seed = (seed ^ 61u) ^ (seed >> 16u);
    seed *= 9u;
    seed = seed ^ (seed >> 4u);
    seed *= 0x27d91955u; 
    seed = seed ^ (seed >> 15u);
    
	return float(seed) * (1.0 / 4294967296.0);
}

void start() {
	float randomAngle = rand(NUMBER) * 2.0 * PI;
	float s = sin(randomAngle);
	float c = cos(randomAngle);
	vec3 originalX = TRANSFORM[0].xyz;
	vec3 originalZ = TRANSFORM[2].xyz;
	
	TRANSFORM[0].xyz = originalX * c + originalZ * s;
	TRANSFORM[2].xyz = originalZ * c + originalX * s;
	
	CUSTOM[0] = 0.0; // Wiek poczatkowy czastki
	COLOR.xyz = texture(CollorPallete, vec2(rand(NUMBER))).xyz; // Kolor
	TRANSFORM = EMISSION_TRANSFORM; // Konwersja na wspolrzedne gloablne
	
	// Skala stratowa
	TRANSFORM[0].xyz = vec3(1., 0.0, 0.0); // wektor bazowy x
	TRANSFORM[1].xyz = vec3(0.0, 1., 0.0); // wektor bazowy y
	TRANSFORM[2].xyz = vec3(0.0, 0.0, 1.); // wektor bazowy z
	
	// Pozycja startowa
	TRANSFORM[3].xyz += vec3(
		((rand(NUMBER) - 1.0) + 0.5) * .5,
		rand(NUMBER - 4u) * 0.25,
		0.0); 

	// Ped poczatkowy
	VELOCITY = vec3(
		(rand(NUMBER - 1u) - .5) * 16. * SnowSpreadWidth,
		(rand(NUMBER + 3u)) * (rand(NUMBER) + 1.),
		(rand(NUMBER - 12u) + 1.)) * SnowMobileVelocity ;
}

void process() {
	if (RESTART) {
		
	}
	
	// Zmiejszenie pedu i zwiekszenie skali
	VELOCITY.y -= rand(NUMBER - 4u) / 5. * DELTA;
	TRANSFORM[0].x +=  1.0;
	TRANSFORM[1].y +=  1.0;
	TRANSFORM[2].z +=  1.0;
	
	// Dlugosc zycia czastki
	CUSTOM[0] += DELTA / LIFETIME;
}