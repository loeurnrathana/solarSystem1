using UnityEngine;

namespace SolarSystemScope
{
    public static class ProceduralPlanetTextures
    {
        private static void Apply4KQuality(Texture2D tex)
        {
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 16;
            tex.wrapModeU = TextureWrapMode.Repeat;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.Apply(true, false);
        }

        public static Texture2D CreateMilkyWaySkyboxTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    // Diagonal Milky Way dust lane angle
                    float bandX = u * 1.5f + v * 0.8f;
                    float distFromGalacticPlane = Mathf.Abs(Mathf.Sin(bandX * Mathf.PI));

                    // Multi-octave cosmic dust turbulence
                    float n1 = Mathf.PerlinNoise(u * 6f, v * 6f);
                    float n2 = Mathf.PerlinNoise(u * 14f + 5f, v * 14f + 2f) * 0.4f;
                    float n3 = Mathf.PerlinNoise(u * 32f + 10f, v * 32f + 8f) * 0.2f;
                    float dustPattern = (n1 + n2 + n3);

                    float galacticIntensity = Mathf.Clamp01(1.0f - distFromGalacticPlane * 2.2f) * dustPattern;

                    // Deep space base color (Dark void blue-black)
                    Color deepSpace = new Color(0.02f, 0.01f, 0.05f, 1.0f);

                    // Cosmic dust cloud colors (Purple, Magenta, Amber, Golden Glow)
                    Color purpleDust = new Color(0.25f, 0.08f, 0.35f, 1.0f);
                    Color magentaNebula = new Color(0.52f, 0.15f, 0.45f, 1.0f);
                    Color goldenCore = new Color(0.85f, 0.52f, 0.18f, 1.0f);

                    Color cosmicBg = deepSpace;
                    if (galacticIntensity > 0.05f)
                    {
                        Color nebulaCol = Color.Lerp(purpleDust, magentaNebula, n1);
                        if (galacticIntensity > 0.45f)
                        {
                            nebulaCol = Color.Lerp(nebulaCol, goldenCore, (galacticIntensity - 0.45f) * 1.8f);
                        }
                        cosmicBg = Color.Lerp(deepSpace, nebulaCol, galacticIntensity);
                    }

                    // Embedded micro-stars within cosmic dust
                    float starNoise = Mathf.PerlinNoise(u * 120f + 50f, v * 120f + 50f);
                    if (starNoise > 0.82f)
                    {
                        float starBright = (starNoise - 0.82f) * 5.5f;
                        Color starCol = Color.Lerp(new Color(0.8f, 0.9f, 1.0f), new Color(1.0f, 0.85f, 0.6f), n2);
                        cosmicBg += starCol * starBright * 0.7f;
                    }

                    colors[y * width + x] = cosmicBg;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateSunTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    
                    // Multi-octave solar plasma turbulence
                    float p1 = Mathf.PerlinNoise(u * 12f, v * 12f);
                    float p2 = Mathf.PerlinNoise(u * 28f + 3.1f, v * 28f + 7.4f) * 0.4f;
                    float p3 = Mathf.PerlinNoise(u * 60f + 12.8f, v * 60f + 9.2f) * 0.15f;
                    float plasma = p1 + p2 + p3;

                    // Solar Granulation (Hot core cells with darker boundaries)
                    float granulation = Mathf.Abs(Mathf.Sin(p2 * 25f)) * 0.25f;

                    // Fiery golden-yellow photospheric plasma matching cinematic reference image
                    Color deepOrangePlasma = new Color(0.98f, 0.40f, 0.05f);
                    Color goldenYellowSun = new Color(1.0f, 0.82f, 0.15f);
                    Color whiteHotFlare = new Color(1.0f, 0.98f, 0.70f);

                    Color sunColor = Color.Lerp(deepOrangePlasma, goldenYellowSun, plasma - granulation);

                    // High-energy white-hot solar flares & granule peaks
                    if (plasma > 0.95f)
                    {
                        sunColor = Color.Lerp(sunColor, whiteHotFlare, (plasma - 0.95f) * 2.8f);
                    }

                    colors[y * width + x] = sunColor;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateSunCoronaTexture(int size = 512)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRad = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRad;
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    
                    // Soft exponential falloff for zero hard steps
                    float rays = Mathf.PerlinNoise(angle * 6f + 10f, dist * 8f) * 0.25f;
                    float alpha = Mathf.Exp(-dist * 3.5f) * (0.95f + rays);
                    alpha = Mathf.Clamp01(alpha);

                    if (dist > 0.98f) alpha = 0f;

                    Color whiteHotCore = new Color(1.0f, 0.95f, 0.75f, alpha);
                    Color goldenAura = new Color(1.0f, 0.65f, 0.10f, alpha * 0.85f);
                    Color deepAmberHalo = new Color(0.95f, 0.32f, 0.02f, alpha * 0.45f);

                    Color coronaColor = Color.Lerp(whiteHotCore, goldenAura, dist * 1.8f);
                    if (dist > 0.45f)
                    {
                        coronaColor = Color.Lerp(coronaColor, deepAmberHalo, (dist - 0.45f) * 1.8f);
                    }
                    coronaColor.a = alpha;
                    colors[y * size + x] = coronaColor;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateMercuryTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    // Multi-scale mineral noise (Matching NASA Messenger false-color map)
                    float n1 = Mathf.PerlinNoise(u * 8f + 1.5f, v * 8f + 2.3f);
                    float n2 = Mathf.PerlinNoise(u * 22f + 5f, v * 22f + 7f) * 0.35f;
                    float n3 = Mathf.PerlinNoise(u * 45f + 12f, v * 45f + 18f) * 0.15f;
                    float val = n1 + n2 + n3;

                    // High-contrast crater ray pattern (Debussy & Caloris basin rays)
                    float craters = Mathf.Pow(Mathf.PerlinNoise(u * 55f + 10f, v * 55f + 14f), 3.8f) * 0.50f;

                    // Natural cratered regolith palette (Matching NASA Messenger natural-color photo)
                    Color darkBasaltLowland = new Color(0.32f, 0.30f, 0.28f);     // Dark grey basaltic crater basins
                    Color regolithHighland = new Color(0.55f, 0.52f, 0.48f);      // Medium silver-grey highland terrain
                    Color rayedCraterEjecta = new Color(0.85f, 0.82f, 0.78f);     // Bright white/grey impact crater rays

                    Color c = Color.Lerp(darkBasaltLowland, regolithHighland, val * 1.2f);
                    if (val > 0.52f)
                    {
                        c = Color.Lerp(c, regolithHighland, (val - 0.52f) * 2.2f);
                    }
                    if (craters > 0.18f)
                    {
                        c = Color.Lerp(c, rayedCraterEjecta, (craters - 0.18f) * 3.8f);
                    }

                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateVenusTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    // V-shaped planetary wind cloud pattern
                    float vFlow = v + Mathf.Abs(u - 0.5f) * 0.35f;
                    float n1 = Mathf.PerlinNoise(u * 6f + Mathf.Sin(vFlow * 10f) * 0.5f, vFlow * 6f);
                    float n2 = Mathf.PerlinNoise(u * 16f, v * 16f) * 0.30f;
                    float val = Mathf.Clamp01(n1 + n2);

                    Color deepAmberCloud = new Color(0.88f, 0.84f, 0.76f); // Pale cream ivory
                    Color lightCreamCloud = new Color(0.95f, 0.92f, 0.86f); // Light cream
                    Color brightSulfuricWhite = new Color(0.98f, 0.96f, 0.92f); // Pure white cloud tops

                    Color c = Color.Lerp(deepAmberCloud, lightCreamCloud, val);
                    if (val > 0.60f)
                    {
                        c = Color.Lerp(c, brightSulfuricWhite, (val - 0.60f) * 2.2f);
                    }
                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateEarthTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            Color deepAbyssalOcean = new Color(0.02f, 0.16f, 0.45f);  // Deep abyssal ocean blue
            Color shallowCoastalBlue = new Color(0.08f, 0.48f, 0.65f); // Caribbean shallow coastal shelf
            Color lushRainforestGreen = new Color(0.10f, 0.42f, 0.16f);// Amazon & Congo rainforest green
            Color temperateForestGreen = new Color(0.20f, 0.48f, 0.22f);// North American / Eurasian temperate forest
            Color saharaGoldenDesert = new Color(0.82f, 0.70f, 0.42f);  // Sahara & Arabian golden sand desert
            Color polarIceCap = new Color(0.96f, 0.98f, 1.0f);        // Arctic & Antarctic ice sheet
            Color atmosphericCloud = new Color(0.98f, 0.99f, 1.0f);   // White swirling cyclone clouds

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float latitude = (v - 0.5f) * 2f; // [-1, 1]
                float absLat = Mathf.Abs(latitude);

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    
                    // Realistic Earth continent geography (Americas, Europe, Africa, Asia, Australia)
                    float n1 = Mathf.PerlinNoise(u * 3.8f + 1.2f, v * 3.8f + 0.8f);
                    float n2 = Mathf.PerlinNoise(u * 8.5f + 5.5f, v * 8.5f + 3.2f) * 0.35f;
                    float n3 = Mathf.PerlinNoise(u * 20f + 12f, v * 20f + 8f) * 0.15f;
                    float continentVal = n1 + n2 + n3;

                    // Swirling cyclone cloud streaks matching NASA Blue Marble photo
                    float cloudN1 = Mathf.PerlinNoise(u * 8f + 10f, v * 8f + 5f);
                    float cloudN2 = Mathf.PerlinNoise(u * 22f + 25f, v * 22f + 18f) * 0.45f;
                    float cloudVal = cloudN1 + cloudN2;

                    Color pixelColor;
                    if (continentVal > 0.51f)
                    {
                        float landElev = (continentVal - 0.51f) * 2.2f;

                        // Smooth organic desert and forest biome blending
                        float desertNoise = Mathf.PerlinNoise(u * 12f + 4f, v * 12f + 2f);
                        Color landBase = Color.Lerp(temperateForestGreen, lushRainforestGreen, 1f - absLat);
                        if (absLat < 0.45f && desertNoise > 0.52f)
                        {
                            landBase = Color.Lerp(landBase, saharaGoldenDesert, (desertNoise - 0.52f) * 2.2f);
                        }

                        pixelColor = landBase;
                        if (landElev > 0.65f)
                        {
                            pixelColor = Color.Lerp(pixelColor, polarIceCap, (landElev - 0.65f) * 2.2f);
                        }
                    }
                    else
                    {
                        // Deep royal abyssal ocean with turquoise coastal shelf (Matching NASA Blue Marble photo)
                        float depth = (0.51f - continentVal) * 2.2f;
                        pixelColor = Color.Lerp(shallowCoastalBlue, deepAbyssalOcean, Mathf.Clamp01(depth * 1.5f));
                    }

                    // Natural North & South Polar Ice Caps (Antarctica & Arctic)
                    if (absLat > 0.82f)
                    {
                        float polarFactor = Mathf.Clamp01((absLat - 0.82f) * 5.5f);
                        pixelColor = Color.Lerp(pixelColor, polarIceCap, polarFactor);
                    }

                    // Swirling white cloud streaks (Matching NASA Blue Marble photo)
                    if (cloudVal > 0.65f)
                    {
                        float cloudAlpha = (cloudVal - 0.65f) * 2.5f;
                        pixelColor = Color.Lerp(pixelColor, atmosphericCloud, Mathf.Clamp01(cloudAlpha * 0.60f));
                    }

                    colors[y * width + x] = pixelColor;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateMoonTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    
                    float n1 = Mathf.PerlinNoise(u * 10f, v * 10f);
                    float n2 = Mathf.PerlinNoise(u * 25f + 4f, v * 25f + 8f) * 0.35f;
                    float craters = Mathf.Pow(Mathf.PerlinNoise(u * 50f + 12f, v * 50f + 15f), 3.5f) * 0.45f;
                    float noise = n1 * 0.6f + n2;

                    Color regolithSilver = new Color(0.72f, 0.72f, 0.75f); // Silver-grey lunar highlands
                    Color darkBasalticMaria = new Color(0.38f, 0.38f, 0.42f); // Lunar maria (Sea of Tranquility)
                    Color rayedCrater = new Color(0.92f, 0.92f, 0.96f); // High-contrast crater rays (Tycho)

                    Color c = Color.Lerp(darkBasalticMaria, regolithSilver, noise * 1.2f);
                    if (craters > 0.20f)
                    {
                        c = Color.Lerp(c, rayedCrater, (craters - 0.20f) * 3.2f);
                    }
                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateMarsTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    // Multi-scale terrain noise (Matching NASA photo of Mars)
                    float n1 = Mathf.PerlinNoise(u * 5f + 1.2f, v * 5f + 0.8f);
                    float n2 = Mathf.PerlinNoise(u * 14f + 3f, v * 14f + 2f) * 0.35f;
                    float n3 = Mathf.PerlinNoise(u * 28f + 8f, v * 28f + 5f) * 0.15f;
                    float terrainVal = n1 + n2 + n3;

                    // Color palette matching NASA reference photo
                    Color darkBasaltMaria = new Color(0.28f, 0.32f, 0.28f);  // Dark greenish-grey basaltic maria (Syrtis Major)
                    Color butterscotchDust = new Color(0.85f, 0.62f, 0.38f); // Warm ochre-tan dusty surface
                    Color paleHighlandDesert = new Color(0.92f, 0.74f, 0.52f); // Pale dust plains (Tharsis)

                    Color c = Color.Lerp(darkBasaltMaria, butterscotchDust, terrainVal * 1.3f);
                    if (terrainVal > 0.55f)
                    {
                        c = Color.Lerp(c, paleHighlandDesert, (terrainVal - 0.55f) * 1.8f);
                    }

                    // Valles Marineris canyon rift system
                    float canyonY = Mathf.Abs(v - 0.48f) * 14f;
                    float canyonX = Mathf.Abs(u - 0.44f) * 3.5f;
                    if (canyonY < 0.15f && canyonX < 0.55f)
                    {
                        c = Color.Lerp(c, new Color(0.22f, 0.18f, 0.14f), 0.75f);
                    }

                    // Tharsis Montes & Olympus Mons volcano caldera rings (Matching reference photo)
                    float v1X = (u - 0.28f) * 3f; float v1Y = (v - 0.55f) * 6f;
                    float v1Dist = Mathf.Sqrt(v1X * v1X + v1Y * v1Y);
                    if (v1Dist < 0.08f)
                    {
                        float rim = Mathf.Abs(v1Dist - 0.05f) * 18f;
                        c = Color.Lerp(new Color(0.95f, 0.80f, 0.62f), c, Mathf.Clamp01(rim));
                    }

                    float v2X = (u - 0.34f) * 3f; float v2Y = (v - 0.46f) * 6f;
                    float v2Dist = Mathf.Sqrt(v2X * v2X + v2Y * v2Y);
                    if (v2Dist < 0.06f)
                    {
                        float rim = Mathf.Abs(v2Dist - 0.04f) * 22f;
                        c = Color.Lerp(new Color(0.95f, 0.80f, 0.62f), c, Mathf.Clamp01(rim));
                    }

                    // Soft Martian North/South polar ice caps
                    if (v > 0.90f)
                    {
                        c = Color.Lerp(c, new Color(0.95f, 0.96f, 0.98f), (v - 0.90f) * 10f);
                    }

                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateJupiterTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float polar = Mathf.Abs(v - 0.5f) * 2f;

                // Alternating cloud belt wave pattern (Matching NASA Hubble photo)
                float bandPattern = Mathf.Sin(v * 38f + Mathf.Cos(v * 8f) * 0.6f) * 0.5f + 0.5f;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    
                    // Turbulent cloud vortices
                    float n1 = Mathf.PerlinNoise(u * 14f, v * 30f) * 0.35f;
                    float n2 = Mathf.PerlinNoise(u * 38f + 4f, v * 50f + 2f) * 0.15f;
                    float n3 = Mathf.PerlinNoise(u * 80f + 12f, v * 90f + 8f) * 0.08f;
                    float mix = bandPattern * 0.55f + n1 + n2 + n3;

                    // Color palette matching NASA Hubble photograph
                    Color pearlWhiteZone = new Color(0.95f, 0.94f, 0.90f);    // Pearl white atmospheric zones
                    Color softCreamZone = new Color(0.90f, 0.84f, 0.74f);     // Soft cream equatorial bands
                    Color terraCottaBelt = new Color(0.85f, 0.52f, 0.34f);    // Warm terra-cotta / salmon cloud belts
                    Color copperBelt = new Color(0.72f, 0.42f, 0.25f);        // Deep copper cloud belts
                    Color goldenOlivePolar = new Color(0.72f, 0.68f, 0.52f);   // Golden-olive polar hoods

                    Color c = Color.Lerp(copperBelt, terraCottaBelt, mix * 1.4f);
                    if (mix > 0.42f)
                    {
                        c = Color.Lerp(c, softCreamZone, (mix - 0.42f) * 1.8f);
                    }
                    if (mix > 0.72f)
                    {
                        c = Color.Lerp(c, pearlWhiteZone, (mix - 0.72f) * 2.2f);
                    }

                    // Golden-olive polar caps at North and South poles (Matching Hubble photo)
                    if (polar > 0.78f)
                    {
                        c = Color.Lerp(c, goldenOlivePolar, (polar - 0.78f) * 4.2f);
                    }

                    // Great Red Spot Oval Storm (Southern Hemisphere v ~ 0.30, u ~ 0.65)
                    float spotX = (u - 0.65f) * 2.2f;
                    float spotY = (v - 0.30f) * 4.4f;
                    float spotDist = Mathf.Sqrt(spotX * spotX + spotY * spotY);
                    if (spotDist < 0.16f)
                    {
                        Color salmonCore = new Color(0.94f, 0.54f, 0.24f);     // Salmon-orange core matching Hubble photo
                        Color whiteRing = new Color(0.96f, 0.96f, 0.92f);      // White surrounding storm boundary ring
                        float ringFactor = Mathf.Abs(spotDist - 0.10f) * 18f;
                        Color redSpotCol = Color.Lerp(salmonCore, whiteRing, Mathf.Clamp01(ringFactor));

                        float stormBlend = Mathf.Clamp01(spotDist / 0.16f);
                        c = Color.Lerp(redSpotCol, c, stormBlend);
                    }

                    // Trailing salmon cloud rift to the right of Great Red Spot
                    if (v > 0.27f && v < 0.33f && u > 0.66f && u < 0.85f)
                    {
                        float riftAlpha = Mathf.Sin((v - 0.27f) / 0.06f * Mathf.PI) * (1f - (u - 0.66f) / 0.19f) * 0.65f;
                        c = Color.Lerp(c, new Color(0.88f, 0.52f, 0.32f), riftAlpha);
                    }

                    // Southern White Oval Storm Spots (Matching Hubble photo)
                    float ovalX1 = (u - 0.35f) * 4f; float ovalY1 = (v - 0.22f) * 8f;
                    if (ovalX1 * ovalX1 + ovalY1 * ovalY1 < 0.04f)
                    {
                        c = Color.Lerp(c, new Color(0.96f, 0.96f, 0.94f), 0.70f);
                    }

                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateSaturnTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float polar = Mathf.Abs(v - 0.5f) * 2f;
                float band = Mathf.Sin(v * 28f) * 0.5f + 0.5f;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float n = Mathf.PerlinNoise(u * 12f, v * 18f) * 0.12f;

                    Color goldenButter = new Color(0.88f, 0.78f, 0.54f);
                    Color tanAtmosphere = new Color(0.72f, 0.58f, 0.38f);
                    Color c = Color.Lerp(tanAtmosphere, goldenButter, band + n);

                    // Soft polar teal/blue atmospheric tint
                    if (polar > 0.85f)
                    {
                        c = Color.Lerp(c, new Color(0.42f, 0.62f, 0.65f), (polar - 0.85f) * 3.5f);
                    }

                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateUranusTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            Color deepSpaceAzure = new Color(0.12f, 0.48f, 0.72f);    // Deep atmospheric limb
            Color cyanAquamarine = new Color(0.24f, 0.82f, 0.92f);   // Core atmosphere
            Color paleIceCyan = new Color(0.55f, 0.94f, 0.98f);       // Pale methane cloud bands
            Color polarMethaneCap = new Color(0.85f, 0.97f, 1.0f);     // Bright polar cap (JWST/Voyager)

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float latitude = (v - 0.5f) * 2f; // [-1, 1]

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    
                    float n1 = Mathf.PerlinNoise(u * 6f, v * 16f) * 0.12f;
                    float n2 = Mathf.PerlinNoise(u * 20f + 3f, v * 40f + 8f) * 0.06f;
                    float bandNoise = n1 + n2;

                    // Base atmospheric sphere gradient
                    float baseGradient = Mathf.Cos(latitude * Mathf.PI * 0.5f);
                    Color c = Color.Lerp(deepSpaceAzure, cyanAquamarine, baseGradient * 0.85f + bandNoise);

                    // Subtle equatorial cloud bands
                    if (Mathf.Abs(latitude) < 0.45f)
                    {
                        float bandStrength = Mathf.Sin(latitude * Mathf.PI * 8f) * 0.08f + 0.08f;
                        c = Color.Lerp(c, paleIceCyan, bandStrength);
                    }

                    // Bright North Polar Methane Ice Cap (Matching NASA JWST / Voyager 2 photos)
                    if (latitude > 0.45f)
                    {
                        float polarFactor = Mathf.Clamp01((latitude - 0.45f) * 2.2f);
                        c = Color.Lerp(c, polarMethaneCap, polarFactor * 0.75f);
                    }

                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateSaturnRingsTexture(int size = 1024)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRad = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRad;
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float ringNoise = Mathf.PerlinNoise(dist * 50f, angle * 4f) * 0.25f;

                    // 100% Transparent empty space in center (dist < 0.42), full 360 ring band (dist in [0.42, 0.95])
                    float alpha = 0f;
                    if (dist >= 0.42f && dist <= 0.95f)
                    {
                        float ringNorm = (dist - 0.42f) / 0.53f;
                        alpha = Mathf.Sin(ringNorm * Mathf.PI) * (0.75f + ringNoise);

                        // Cassini Division (Dark gap in Saturn's rings)
                        if (ringNorm > 0.58f && ringNorm < 0.66f) alpha *= 0.05f;
                    }

                    Color ringColor = Color.Lerp(new Color(0.68f, 0.58f, 0.42f, alpha), new Color(0.88f, 0.82f, 0.68f, alpha), ringNoise);
                    colors[y * size + x] = ringColor;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateUranusRingsTexture(int size = 1024)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRad = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRad;
                    float alpha = 0f;

                    // Delicate, realistic fine concentric ring structure (Matching NASA Hubble/Voyager photo)
                    if (dist >= 0.55f && dist <= 0.92f)
                    {
                        float ringNorm = (dist - 0.55f) / 0.37f;
                        
                        // Narrow distinct ring strands (Epsilon ring, Eta ring, etc.)
                        float strand1 = Mathf.SmoothStep(0.02f, 0.0f, Mathf.Abs(ringNorm - 0.25f));
                        float strand2 = Mathf.SmoothStep(0.015f, 0.0f, Mathf.Abs(ringNorm - 0.48f));
                        float strand3 = Mathf.SmoothStep(0.025f, 0.0f, Mathf.Abs(ringNorm - 0.72f));
                        float strand4 = Mathf.SmoothStep(0.035f, 0.0f, Mathf.Abs(ringNorm - 0.88f));

                        alpha = Mathf.Clamp01((strand1 * 0.45f + strand2 * 0.35f + strand3 * 0.50f + strand4 * 0.65f));
                    }

                    Color ringColor = new Color(0.70f, 0.92f, 0.98f, alpha); // Silvery cyan ring ice
                    colors[y * size + x] = ringColor;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateNeptuneTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float n1 = Mathf.PerlinNoise(u * 8f, v * 16f) * 0.25f;
                    float n2 = Mathf.PerlinNoise(u * 22f + 5f, v * 40f + 3f) * 0.12f;
                    float cloud = Mathf.PerlinNoise(u * 32f + 15f, v * 32f + 8f);

                    Color deepCobaltAbyss = new Color(0.28f, 0.48f, 0.75f); // True-color pale azure abyss
                    Color vibrantRoyalBlue = new Color(0.42f, 0.65f, 0.88f); // Soft azure blue
                    Color c = Color.Lerp(deepCobaltAbyss, vibrantRoyalBlue, n1 + n2 + v * 0.2f);

                    // Great Dark Spot Storm (Matching Voyager 2 photo)
                    float spotDistX = (u - 0.45f) * 2.5f;
                    float spotDistY = (v - 0.35f) * 4.0f;
                    float spotDist = Mathf.Sqrt(spotDistX * spotDistX + spotDistY * spotDistY);
                    if (spotDist < 0.15f)
                    {
                        Color darkSpotCore = new Color(0.03f, 0.08f, 0.35f);
                        Color methaneBrightBorder = new Color(0.92f, 0.96f, 1.0f);
                        float borderFactor = Mathf.Abs(spotDist - 0.11f) * 22f;
                        Color spotCol = Color.Lerp(darkSpotCore, methaneBrightBorder, Mathf.Clamp01(borderFactor));
                        c = Color.Lerp(spotCol, c, Mathf.Clamp01(spotDist / 0.15f));
                    }

                    // Bright white cirrus methane clouds (Scooter clouds)
                    if (cloud > 0.72f)
                    {
                        c = Color.Lerp(c, new Color(0.96f, 0.98f, 1.0f), (cloud - 0.72f) * 3.2f);
                    }

                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreatePlutoTexture(int width = 4096, int height = 2048)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] colors = new Color[width * height];

            Color tholinDarkRed = new Color(0.32f, 0.12f, 0.08f);    // Cthulhu Macula dark tholin
            Color tholinOchre = new Color(0.68f, 0.38f, 0.22f);      // Reddish-ochre terrain
            Color nitrogenIceWhite = new Color(0.96f, 0.95f, 0.93f); // Tombaugh Regio heart glacier
            Color nitrogenIceCream = new Color(0.88f, 0.82f, 0.74f); // Ice plains
            Color polarIceCap = new Color(0.92f, 0.94f, 0.96f);      // Methane/nitrogen polar cap

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float latitude = Mathf.Abs(v - 0.5f) * 2f;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    float n1 = Mathf.PerlinNoise(u * 6f + 2f, v * 6f + 1f);
                    float n2 = Mathf.PerlinNoise(u * 14f + 8f, v * 14f + 5f) * 0.35f;
                    float n3 = Mathf.PerlinNoise(u * 30f + 12f, v * 30f + 7f) * 0.15f;
                    float terrainNoise = n1 + n2 + n3;

                    // Base tholin reddish-brown terrain gradient
                    Color c = Color.Lerp(tholinDarkRed, tholinOchre, Mathf.Clamp01(terrainNoise));

                    // Iconic Heart Glacier (Tombaugh Regio / Sputnik Planitia matching New Horizons photo)
                    float heartDx = (u - 0.52f) * 3.2f;
                    float heartDy = (v - 0.46f) * 3.2f;
                    float hx = heartDx;
                    float hy = heartDy + 0.12f;
                    float heartFormula = Mathf.Pow(hx * hx + hy * hy - 0.08f, 3f) - hx * hx * Mathf.Pow(hy, 3f);

                    if (heartFormula < 0.005f)
                    {
                        float heartFactor = Mathf.Clamp01(-heartFormula * 80f);
                        Color heartCol = Color.Lerp(nitrogenIceCream, nitrogenIceWhite, heartFactor);
                        c = Color.Lerp(c, heartCol, Mathf.Clamp01((0.005f - heartFormula) * 120f));
                    }

                    // Bright North & South Polar Ice Caps
                    if (latitude > 0.85f)
                    {
                        c = Color.Lerp(c, polarIceCap, (latitude - 0.85f) * 6.5f);
                    }

                    colors[y * width + x] = c;
                }
            }

            tex.SetPixels(colors);
            Apply4KQuality(tex);
            return tex;
        }

        public static Texture2D CreateHandCursorTexture(int size = 32)
        {
            if (size < 16) size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

            Color handFill = new Color(0.98f, 0.98f, 1.0f, 0.95f);
            Color handOutline = new Color(0.10f, 0.12f, 0.18f, 0.95f);

            float s = size / 32f;
            int minYIndex = Mathf.Clamp(Mathf.RoundToInt(14 * s), 0, size - 1);
            int maxYIndex = Mathf.Clamp(Mathf.RoundToInt(28 * s), 0, size - 1);
            int minXIndex = Mathf.Clamp(Mathf.RoundToInt(12 * s), 0, size - 1);
            int maxXIndex = Mathf.Clamp(Mathf.RoundToInt(16 * s), 0, size - 1);

            // Index finger (pointing up)
            for (int y = minYIndex; y <= maxYIndex; y++)
            {
                for (int x = minXIndex; x <= maxXIndex; x++)
                {
                    colors[y * size + x] = handFill;
                }
            }

            int minYPalm = Mathf.Clamp(Mathf.RoundToInt(4 * s), 0, size - 1);
            int maxYPalm = Mathf.Clamp(Mathf.RoundToInt(16 * s), 0, size - 1);
            int minXPalm = Mathf.Clamp(Mathf.RoundToInt(8 * s), 0, size - 1);
            int maxXPalm = Mathf.Clamp(Mathf.RoundToInt(22 * s), 0, size - 1);

            // Palm base
            for (int y = minYPalm; y <= maxYPalm; y++)
            {
                for (int x = minXPalm; x <= maxXPalm; x++)
                {
                    colors[y * size + x] = handFill;
                }
            }

            int minYThumb = Mathf.Clamp(Mathf.RoundToInt(8 * s), 0, size - 1);
            int maxYThumb = Mathf.Clamp(Mathf.RoundToInt(16 * s), 0, size - 1);
            int minXThumb = Mathf.Clamp(Mathf.RoundToInt(4 * s), 0, size - 1);
            int maxXThumb = Mathf.Clamp(Mathf.RoundToInt(9 * s), 0, size - 1);

            // Thumb
            for (int y = minYThumb; y <= maxYThumb; y++)
            {
                for (int x = minXThumb; x <= maxXThumb; x++)
                {
                    colors[y * size + x] = handFill;
                }
            }

            // Add crisp dark outline around hand boundary
            Color[] temp = (Color[])colors.Clone();
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    if (temp[y * size + x].a > 0.1f)
                    {
                        if (temp[y * size + (x + 1)].a < 0.1f || temp[y * size + (x - 1)].a < 0.1f ||
                            temp[(y + 1) * size + x].a < 0.1f || temp[(y - 1) * size + x].a < 0.1f)
                        {
                            colors[y * size + x] = handOutline;
                        }
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }
    }
}
