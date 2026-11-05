using System.Text;
using NawayasaCitramandala.Core;

namespace NawayasaCitramandala.Imaging;

/// <summary>
/// Generates interactive 3D WebGL/Three.js simulation environments modeling
/// microscopic optical diffraction grating iridescence (rainbow sheen) on burned discs.
/// </summary>
public static class Disc3DVisualizer
{
    public static string GenerateHtmlViewer(
        string discTitle,
        string imageBase64Png,
        OpticalTrackStandard mediaType = OpticalTrackStandard.DvdMinusR,
        double innerRadiusMm = 24.0,
        double outerRadiusMm = 58.0)
    {
        var spec = MediaFormatSpecification.GetSpecification(mediaType);
        double trackPitchMicrons = spec.TrackPitchMicrons;

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <title>{{discTitle}} - Nawayasa Citramandala 3D Simulation</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { background: #0a0c10; color: #e2e8f0; font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; overflow: hidden; }
    #canvas-container { width: 100vw; height: 100vh; position: absolute; }
    #overlay {
      position: absolute; top: 20px; left: 20px; z-index: 10;
      background: rgba(15, 23, 42, 0.85); backdrop-filter: blur(12px);
      padding: 18px 24px; border-radius: 12px; border: 1px solid rgba(255, 255, 255, 0.1);
      box-shadow: 0 20px 40px rgba(0,0,0,0.5); max-width: 360px;
    }
    h1 { font-size: 1.1rem; color: #38bdf8; margin-bottom: 6px; font-weight: 600; }
    p { font-size: 0.85rem; color: #94a3b8; line-height: 1.4; margin-bottom: 12px; }
    .badge { display: inline-block; background: #0284c7; color: white; padding: 2px 8px; border-radius: 4px; font-size: 0.75rem; font-weight: 600; margin-bottom: 8px; }
    .controls { display: flex; flex-direction: column; gap: 8px; font-size: 0.8rem; }
    .ctrl-row { display: flex; justify-content: space-between; align-items: center; }
    input[type=range] { width: 140px; accent-color: #38bdf8; }
    .hint { font-size: 0.75rem; color: #64748b; margin-top: 10px; }
  </style>
  <script src="https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js"></script>
  <script src="https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js"></script>
</head>
<body>
  <div id="canvas-container"></div>
  <div id="overlay">
    <span class="badge">{{spec.DisplayName}}</span>
    <h1>{{discTitle}}</h1>
    <p>Simulasi fotorealistik difraksi kisi optik (optical diffraction grating) berbasis hukum Bragg pada lintasan spiral mikroskopis.</p>
    <div class="controls">
      <div class="ctrl-row">
        <span>Intensitas Difraksi:</span>
        <input type="range" id="iridescence" min="0" max="2" step="0.05" value="1.2">
      </div>
      <div class="ctrl-row">
        <span>Rotasi Otomatis:</span>
        <input type="checkbox" id="auto-rotate" checked>
      </div>
    </div>
    <div class="hint">🖱️ Klik & geser untuk memutar piringan | Scroll untuk zoom</div>
  </div>

  <script>
    const container = document.getElementById('canvas-container');
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(45, window.innerWidth / window.innerHeight, 0.1, 100);
    camera.position.set(0, 0, 16);

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.1;
    container.appendChild(renderer.domElement);

    const controls = new THREE.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.05;

    // Ambient & Directional Lights
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.4);
    scene.add(ambientLight);

    const keyLight = new THREE.DirectionalLight(0xffffff, 1.6);
    keyLight.position.set(5, 8, 10);
    scene.add(keyLight);

    const fillLight = new THREE.PointLight(0x38bdf8, 1.2, 50);
    fillLight.position.set(-8, -6, 5);
    scene.add(fillLight);

    // Texture Loader
    const textureLoader = new THREE.TextureLoader();
    const artworkTexture = textureLoader.load('data:image/png;base64,{{imageBase64Png}}');

    // Shader Material for Disc Surface with Bragg Diffraction Iridescence
    const discMaterial = new THREE.ShaderMaterial({
      uniforms: {
        tArtwork: { value: artworkTexture },
        uLightPos: { value: keyLight.position },
        uTrackPitch: { value: {{trackPitchMicrons.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}} },
        uIridescenceStrength: { value: 1.2 },
        uInnerRadius: { value: {{innerRadiusMm / 60.0}} },
        uOuterRadius: { value: {{outerRadiusMm / 60.0}} }
      },
      vertexShader: `
        varying vec2 vUv;
        varying vec3 vNormal;
        varying vec3 vWorldPos;
        varying vec3 vViewDir;

        void main() {
          vUv = uv;
          vNormal = normalize(normalMatrix * normal);
          vec4 worldPos = modelMatrix * vec4(position, 1.0);
          vWorldPos = worldPos.xyz;
          vViewDir = normalize(cameraPosition - worldPos.xyz);
          gl_Position = projectionMatrix * viewMatrix * worldPos;
        }
      `,
      fragmentShader: `
        uniform sampler2D tArtwork;
        uniform vec3 uLightPos;
        uniform float uTrackPitch;
        uniform float uIridescenceStrength;
        uniform float uInnerRadius;
        uniform float uOuterRadius;

        varying vec2 vUv;
        varying vec3 vNormal;
        varying vec3 vWorldPos;
        varying vec3 vViewDir;

        // Spectral rainbow generator for optical diffraction
        vec3 spectralColor(float t) {
          vec3 w = vec3(t, t - 0.33, t - 0.67);
          return clamp(cos(w * 6.28318) * 0.5 + 0.5, 0.0, 1.0);
        }

        void main() {
          vec2 centerUv = vUv - vec2(0.5);
          float r = length(centerUv) * 2.0;

          // Transparent central hole (< 15mm => r < 0.25)
          if (r < 0.25 || r > 1.0) discard;

          vec4 artColor = texture2D(tArtwork, vUv);
          vec3 L = normalize(uLightPos - vWorldPos);
          vec3 V = normalize(vViewDir);
          vec3 N = normalize(vNormal);

          // Anisotropic tangent along circular tracks
          vec3 tangent = normalize(vec3(-centerUv.y, centerUv.x, 0.0));

          // Bragg diffraction projection
          float LdotT = dot(L, tangent);
          float VdotT = dot(V, tangent);
          float diffractionFactor = abs(LdotT - VdotT) * (1.6 / uTrackPitch);

          vec3 rainbow = spectralColor(fract(diffractionFactor * 1.5)) * uIridescenceStrength;

          // Disc specular shine
          float NdotL = max(dot(N, L), 0.0);
          vec3 H = normalize(L + V);
          float spec = pow(max(dot(N, H), 0.0), 32.0);

          // Modulate artwork contrast over iridescent substrate
          vec3 substrate = vec3(0.12, 0.14, 0.18) + (rainbow * 0.85);
          vec3 finalColor = mix(substrate, artColor.rgb * substrate * 1.6, artColor.a) + (spec * 0.35);

          // Clamping ring & hub styling
          if (r < uInnerRadius) {
            finalColor = vec3(0.08, 0.09, 0.12) + (spec * 0.4);
          }

          gl_FragColor = vec4(finalColor, 1.0);
        }
      `,
      side: THREE.DoubleSide
    });

    // 120mm Disc Geometry
    const discGeometry = new THREE.CylinderGeometry(5.0, 5.0, 0.05, 128);
    discGeometry.rotateX(Math.PI / 2);
    const discMesh = new THREE.Mesh(discGeometry, discMaterial);
    scene.add(discMesh);

    // GUI wiring
    const iridSlider = document.getElementById('iridescence');
    iridSlider.addEventListener('input', (e) => {
      discMaterial.uniforms.uIridescenceStrength.value = parseFloat(e.target.value);
    });

    const autoRotateCheckbox = document.getElementById('auto-rotate');

    // Animation Loop
    function animate() {
      requestAnimationFrame(animate);
      if (autoRotateCheckbox.checked) {
        discMesh.rotation.z += 0.006;
      }
      controls.update();
      renderer.render(scene, camera);
    }

    animate();

    window.addEventListener('resize', () => {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    });
  </script>
</body>
</html>
""";
    }
}
