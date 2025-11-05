// Terra Nova - Three.js Renderer
// This file provides the JavaScript side of the renderer that interfaces with C# via JS Interop

window.terraNovaRenderer = (function() {
    let scene, camera, renderer;
    let chunkMeshes = new Map(); // Map of chunk position keys to mesh objects
    let highlightedBlock = null;
    let noiseTexture = null; // Procedurally generated noise texture from C#

    // Camera controls
    let moveSpeed = 0.05;
    let rotateSpeed = 0.002;
    let keys = {};
    let cameraRotation = { x: 0, y: 0 };

    // FPS limiting
    let targetFPS = 60;
    let frameTime = 1000 / targetFPS; // milliseconds per frame
    let lastFrameTime = 0;

    /**
     * Set the noise texture from C# generated data (using shared TextureGenerator code)
     * @param {Uint8Array} textureData - RGBA pixel data from C#
     * @param {number} size - Texture size (width and height)
     */
    function setNoiseTexture(textureData, size) {
        const data = new Uint8Array(textureData);

        const texture = new THREE.DataTexture(data, size, size, THREE.RGBAFormat);
        texture.needsUpdate = true;

        // Use nearest filtering for pixelated look (matching desktop)
        texture.minFilter = THREE.NearestFilter;
        texture.magFilter = THREE.NearestFilter;
        texture.wrapS = THREE.RepeatWrapping;
        texture.wrapT = THREE.RepeatWrapping;

        // Use linear color space to match desktop OpenGL
        texture.colorSpace = THREE.LinearSRGBColorSpace;

        noiseTexture = texture;

        // Update block interaction system with the texture
        if (window.terraNovaBlockInteraction) {
            window.terraNovaBlockInteraction.setTexture(texture);
        }

        console.log('Noise texture set from C# shared code (16x16, grayscale 200-255)');
    }

    /**
     * Initialize the Three.js scene, camera, and renderer
     */
    function init() {
        console.log('Initializing Three.js renderer...');

        // Get the canvas element
        const canvas = document.getElementById('renderCanvas');
        if (!canvas) {
            console.error('Canvas element not found!');
            return false;
        }

        // Create scene
        scene = new THREE.Scene();
        scene.background = new THREE.Color(0x87CEEB); // Sky blue

        // Create camera with wider FOV for closer viewing
        camera = new THREE.PerspectiveCamera(
            75, // FOV
            window.innerWidth / window.innerHeight, // Aspect ratio
            0.1, // Near plane
            1000 // Far plane
        );
        // Position camera higher and farther to see the terrain layers
        camera.position.set(8, 10, 20);

        // Set rotation order for proper first-person camera behavior
        // YXZ means: rotate around Y (yaw) first, then X (pitch), then Z (roll)
        // This prevents gimbal lock and orbital behavior
        camera.rotation.order = 'YXZ';

        // Initialize camera rotation to look at the terrain center
        const lookAtPoint = new THREE.Vector3(8, 2, 8);
        camera.lookAt(lookAtPoint);

        // Force camera to stay level (no roll/tilt on z-axis)
        camera.rotation.z = 0;

        // Calculate initial rotation from lookAt
        cameraRotation.x = camera.rotation.x;
        cameraRotation.y = camera.rotation.y;

        // Create renderer
        renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true });
        renderer.setSize(window.innerWidth, window.innerHeight);
        renderer.setPixelRatio(window.devicePixelRatio);

        // Use linear color space to match desktop OpenGL (no gamma correction)
        renderer.outputColorSpace = THREE.LinearSRGBColorSpace;

        // Add ambient light
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        scene.add(ambientLight);

        // Add directional light
        const directionalLight = new THREE.DirectionalLight(0xffffff, 0.4);
        directionalLight.position.set(50, 100, 50);
        scene.add(directionalLight);

        // Note: Noise texture will be set from C# via setNoiseTexture()

        // Handle window resize
        window.addEventListener('resize', onWindowResize);

        // Setup keyboard and mouse controls
        setupControls();

        // Start render loop
        animate();

        // Initialize block interaction system
        if (window.terraNovaBlockInteraction) {
            window.terraNovaBlockInteraction.initialize(camera, scene, noiseTexture);
            console.log('Block interaction system initialized');
        }

        console.log('Three.js renderer initialized successfully');
        return true;
    }

    /**
     * Handle window resize events
     */
    function onWindowResize() {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
    }

    /**
     * Setup keyboard and mouse controls
     */
    function setupControls() {
        // Keyboard events
        document.addEventListener('keydown', (e) => {
            keys[e.key.toLowerCase()] = true;
        });

        document.addEventListener('keyup', (e) => {
            keys[e.key.toLowerCase()] = false;
        });

        // Request pointer lock on click anywhere in the document
        document.addEventListener('click', () => {
            if (!document.pointerLockElement) {
                document.body.requestPointerLock();
                console.log('Requesting pointer lock...');
            }
        });

        // Handle pointer lock change
        document.addEventListener('pointerlockchange', () => {
            if (document.pointerLockElement) {
                console.log('Pointer locked - mouse will control camera');
            } else {
                console.log('Pointer unlocked - click anywhere to re-lock');
            }
        });

        // Handle pointer lock errors
        document.addEventListener('pointerlockerror', () => {
            console.error('Pointer lock failed');
        });

        // Mouse movement for camera control (works when pointer is locked)
        document.addEventListener('mousemove', (e) => {
            if (document.pointerLockElement) {
                // Use movementX/Y for pointer lock (gives delta directly)
                const deltaX = e.movementX || 0;
                const deltaY = e.movementY || 0;

                // Apply rotation
                cameraRotation.y -= deltaX * rotateSpeed;
                cameraRotation.x -= deltaY * rotateSpeed;

                // Clamp vertical rotation to prevent camera flipping
                cameraRotation.x = Math.max(-Math.PI / 2, Math.min(Math.PI / 2, cameraRotation.x));
            }
        });

        console.log('Controls initialized - Click to lock mouse, WASD to move, mouse to look, ESC to unlock');
    }

    /**
     * Update camera position based on input
     */
    function updateCamera() {
        // Calculate forward and right vectors based on camera rotation
        const forward = new THREE.Vector3(
            Math.sin(cameraRotation.y),
            0,
            Math.cos(cameraRotation.y)
        );

        const right = new THREE.Vector3(
            Math.cos(cameraRotation.y),
            0,
            -Math.sin(cameraRotation.y)
        );

        // WASD movement (always horizontal, regardless of vertical look angle)
        if (keys['w']) {
            camera.position.sub(forward.clone().multiplyScalar(moveSpeed));
        }
        if (keys['s']) {
            camera.position.add(forward.clone().multiplyScalar(moveSpeed));
        }
        if (keys['a']) {
            camera.position.sub(right.clone().multiplyScalar(moveSpeed));
        }
        if (keys['d']) {
            camera.position.add(right.clone().multiplyScalar(moveSpeed));
        }

        // Space to go up, Shift to go down
        if (keys[' ']) {
            camera.position.y += moveSpeed;
        }
        if (keys['shift']) {
            camera.position.y -= moveSpeed;
        }

        // Apply rotation (always keep z at 0 to prevent roll/tilt)
        camera.rotation.x = cameraRotation.x;
        camera.rotation.y = cameraRotation.y;
        camera.rotation.z = 0;
    }

    /**
     * Animation/render loop with FPS limiting
     */
    function animate(currentTime) {
        requestAnimationFrame(animate);

        // FPS limiting - only render if enough time has passed
        const deltaTime = currentTime - lastFrameTime;
        if (deltaTime < frameTime) {
            return; // Skip this frame
        }

        // Update last frame time (with correction for missed frames)
        lastFrameTime = currentTime - (deltaTime % frameTime);

        // Update and render
        updateCamera();
        renderer.render(scene, camera);
    }

    /**
     * Update or create a chunk mesh (2D column chunk)
     * @param {number} chunkX - Chunk X position
     * @param {number} chunkZ - Chunk Z position
     * @param {Float32Array} vertices - Vertex positions (xyz)
     * @param {Float32Array} colors - Vertex colors (rgb)
     * @param {Float32Array} texCoords - Texture coordinates (uv)
     * @param {Uint32Array} indices - Triangle indices
     */
    function updateChunk(chunkX, chunkZ, vertices, colors, texCoords, indices) {
        const chunkKey = `${chunkX},${chunkZ}`;

        // Remove existing mesh if any
        if (chunkMeshes.has(chunkKey)) {
            const oldMesh = chunkMeshes.get(chunkKey);
            scene.remove(oldMesh);
            oldMesh.geometry.dispose();
            oldMesh.material.dispose();
            chunkMeshes.delete(chunkKey);
        }

        // Skip if no vertices
        if (vertices.length === 0 || indices.length === 0) {
            return;
        }

        // Convert arrays from C# to typed arrays for Three.js
        const verticesTyped = new Float32Array(vertices);
        const colorsTyped = new Float32Array(colors);
        const texCoordsTyped = new Float32Array(texCoords);
        const indicesTyped = new Uint32Array(indices);

        // Create geometry
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.BufferAttribute(verticesTyped, 3));
        geometry.setAttribute('color', new THREE.BufferAttribute(colorsTyped, 3));
        geometry.setAttribute('uv', new THREE.BufferAttribute(texCoordsTyped, 2));
        geometry.setIndex(new THREE.BufferAttribute(indicesTyped, 1));
        geometry.computeVertexNormals();

        // Create material with vertex colors and noise texture (matching desktop)
        const material = new THREE.MeshBasicMaterial({
            vertexColors: true,
            map: noiseTexture,
            side: THREE.DoubleSide
        });

        // Create mesh - vertices are already in world space, so no position offset needed
        const mesh = new THREE.Mesh(geometry, material);
        mesh.position.set(0, 0, 0);

        scene.add(mesh);
        chunkMeshes.set(chunkKey, mesh);

        // Register chunk with block interaction system
        if (window.terraNovaBlockInteraction) {
            window.terraNovaBlockInteraction.registerChunk(chunkKey, mesh);
        }

        console.log(`Updated chunk column (${chunkX}, ${chunkZ}) with ${indices.length / 3} triangles (vertices already in world space)`);
    }

    /**
     * Remove a chunk mesh (2D column chunk)
     * @param {number} chunkX - Chunk X position
     * @param {number} chunkZ - Chunk Z position
     */
    function removeChunk(chunkX, chunkZ) {
        const chunkKey = `${chunkX},${chunkZ}`;

        if (chunkMeshes.has(chunkKey)) {
            const mesh = chunkMeshes.get(chunkKey);
            scene.remove(mesh);
            mesh.geometry.dispose();
            mesh.material.dispose();
            chunkMeshes.delete(chunkKey);

            // Unregister chunk from block interaction system
            if (window.terraNovaBlockInteraction) {
                window.terraNovaBlockInteraction.unregisterChunk(chunkKey);
            }

            console.log(`Removed chunk column (${chunkX}, ${chunkZ})`);
        }
    }

    /**
     * Update camera position and rotation
     * @param {number} posX - Camera X position
     * @param {number} posY - Camera Y position
     * @param {number} posZ - Camera Z position
     * @param {number} rotX - Camera X rotation (euler angles in radians)
     * @param {number} rotY - Camera Y rotation
     * @param {number} rotZ - Camera Z rotation
     */
    function setCamera(posX, posY, posZ, rotX, rotY, rotZ) {
        camera.position.set(posX, posY, posZ);
        camera.rotation.set(rotX, rotY, rotZ);
    }

    /**
     * Highlight a block (for selection)
     * @param {boolean} highlight - Whether to show highlight
     * @param {number} blockX - Block X position
     * @param {number} blockY - Block Y position
     * @param {number} blockZ - Block Z position
     */
    function highlightBlock(highlight, blockX, blockY, blockZ) {
        // Remove existing highlight
        if (highlightedBlock) {
            scene.remove(highlightedBlock);
            highlightedBlock.geometry.dispose();
            highlightedBlock.material.dispose();
            highlightedBlock = null;
        }

        if (highlight) {
            // Create wireframe box for highlight
            const geometry = new THREE.BoxGeometry(1.02, 1.02, 1.02); // Slightly larger than block
            const edges = new THREE.EdgesGeometry(geometry);
            const material = new THREE.LineBasicMaterial({ color: 0x000000, linewidth: 2 });
            highlightedBlock = new THREE.LineSegments(edges, material);
            highlightedBlock.position.set(blockX, blockY, blockZ);
            scene.add(highlightedBlock);
        }
    }

    // Expose public API
    return {
        init: init,
        setNoiseTexture: setNoiseTexture,
        updateChunk: updateChunk,
        removeChunk: removeChunk,
        setCamera: setCamera,
        highlightBlock: highlightBlock
    };
})();

// Auto-initialize when the page loads
window.addEventListener('load', function() {
    terraNovaRenderer.init();
});
