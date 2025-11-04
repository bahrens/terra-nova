// Terra Nova - Block Interaction System
// Handles block selection, placement, and destruction using Three.js raycasting

window.terraNovaBlockInteraction = (function() {
    let raycaster = new THREE.Raycaster();
    let selectedBlock = null;
    let selectionMesh = null; // Mesh with bordered shader for selected block
    let borderedMaterial = null; // Custom shader material for selection
    let camera = null;
    let scene = null;
    let chunks = {}; // Store chunk meshes for raycasting
    let worldData = {}; // Store world block data (x,y,z) -> blockType
    let noiseTexture = null; // Reference to noise texture
    let blockColors = null; // Block colors from C# BlockHelper (single source of truth)

    /**
     * Initialize the block interaction system
     * @param {THREE.Camera} cam - The Three.js camera
     * @param {THREE.Scene} scn - The Three.js scene
     * @param {THREE.Texture} texture - The noise texture to use for rendering (optional, can be set later)
     */
    function initialize(cam, scn, texture) {
        camera = cam;
        scene = scn;
        if (texture) {
            noiseTexture = texture;
        }

        // Create bordered shader material (matches desktop client's bordered.frag)
        borderedMaterial = new THREE.ShaderMaterial({
            uniforms: {
                blockTexture: { value: noiseTexture }
            },
            vertexShader: `
                varying vec2 vTexCoord;
                varying vec3 vVertexColor;

                void main() {
                    vTexCoord = uv;
                    vVertexColor = color;
                    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
                }
            `,
            fragmentShader: `
                uniform sampler2D blockTexture;
                varying vec2 vTexCoord;
                varying vec3 vVertexColor;

                void main() {
                    // Sample the texture
                    vec4 texColor = texture2D(blockTexture, vTexCoord);

                    // Define border thickness (in texture coordinate space, 0.0 to 1.0)
                    float borderWidth = 0.009;
                    float aaWidth = 0.004; // Antialiasing transition width

                    // Calculate distance from edge
                    float distFromEdgeX = min(vTexCoord.x, 1.0 - vTexCoord.x);
                    float distFromEdgeY = min(vTexCoord.y, 1.0 - vTexCoord.y);
                    float distFromEdge = min(distFromEdgeX, distFromEdgeY);

                    // Smooth transition from border to texture
                    float borderMix = smoothstep(borderWidth - aaWidth, borderWidth, distFromEdge);

                    // Mix between border color (black) and textured vertex color
                    vec4 borderColor = vec4(0.0, 0.0, 0.0, 1.0);
                    vec4 blockColor = texColor * vec4(vVertexColor, 1.0);
                    gl_FragColor = mix(borderColor, blockColor, borderMix);
                }
            `,
            vertexColors: true,
            polygonOffset: true,
            polygonOffsetFactor: -1.0,
            polygonOffsetUnits: -1.0
        });

        // Setup mouse event handlers
        document.addEventListener('mousedown', onMouseDown, false);
        document.addEventListener('mousemove', onMouseMove, false);

        console.log('Block interaction system initialized with bordered shader');
    }

    /**
     * Register a chunk mesh for raycasting
     * @param {string} chunkKey - Chunk identifier "x,y,z"
     * @param {THREE.Mesh} mesh - The chunk mesh
     */
    function registerChunk(chunkKey, mesh) {
        chunks[chunkKey] = mesh;
    }

    /**
     * Unregister a chunk mesh
     * @param {string} chunkKey - Chunk identifier "x,y,z"
     */
    function unregisterChunk(chunkKey) {
        delete chunks[chunkKey];
    }

    /**
     * Set block data for the world (used for accurate raycasting)
     * @param {number} x - Block X position
     * @param {number} y - Block Y position
     * @param {number} z - Block Z position
     * @param {number} blockType - Block type (0 = Air)
     */
    function setBlockData(x, y, z, blockType) {
        const key = `${x},${y},${z}`;
        if (blockType === 0) {
            delete worldData[key];
        } else {
            worldData[key] = blockType;
        }
    }

    /**
     * Get block data from world
     * @param {number} x - Block X position
     * @param {number} y - Block Y position
     * @param {number} z - Block Z position
     * @returns {number} Block type (0 if air/not found)
     */
    function getBlockData(x, y, z) {
        const key = `${x},${y},${z}`;
        return worldData[key] || 0;
    }

    /**
     * Update block selection based on camera direction
     */
    function updateSelection() {
        if (!camera) return;

        // Raycast from camera center
        raycaster.setFromCamera(new THREE.Vector2(0, 0), camera);

        // Get all chunk meshes
        const meshes = Object.values(chunks);
        if (meshes.length === 0) {
            clearSelectionMesh();
            selectedBlock = null;
            return;
        }

        // Find intersections
        const intersects = raycaster.intersectObjects(meshes, false);

        if (intersects.length > 0) {
            const intersect = intersects[0];
            const point = intersect.point;
            const normal = intersect.face.normal;

            // Calculate block position by rounding (blocks are centered at integer coords)
            // This matches the desktop client's coordinate system
            const blockX = Math.round(point.x - normal.x * 0.5);
            const blockY = Math.round(point.y - normal.y * 0.5);
            const blockZ = Math.round(point.z - normal.z * 0.5);

            // Check if this block actually exists in our world data
            if (getBlockData(blockX, blockY, blockZ) !== 0) {
                selectedBlock = {
                    x: blockX,
                    y: blockY,
                    z: blockZ,
                    face: {
                        x: Math.round(normal.x),
                        y: Math.round(normal.y),
                        z: Math.round(normal.z)
                    }
                };

                // Update selection mesh with bordered shader
                updateSelectionMesh(blockX, blockY, blockZ);
            } else {
                clearSelectionMesh();
                selectedBlock = null;
            }
        } else {
            clearSelectionMesh();
            selectedBlock = null;
        }
    }

    /**
     * Set block colors from C# (single source of truth)
     * @param {Object} colors - Dictionary of block type to color {r, g, b}
     */
    function setBlockColors(colors) {
        blockColors = colors;
        console.log('Block colors received from C# BlockHelper:', blockColors);
    }

    /**
     * Get block color based on block type (uses colors from C# BlockHelper)
     * @param {number} blockType - Block type enum value
     * @returns {THREE.Color} Block color
     */
    function getBlockColor(blockType) {
        // Use colors from C# if available, otherwise fallback to white
        if (blockColors && blockColors[blockType]) {
            const color = blockColors[blockType];
            return new THREE.Color(color.r, color.g, color.b);
        }

        // Fallback to white if colors not yet loaded
        if (!blockColors) {
            console.warn(`Block colors not loaded yet (selecting block type ${blockType}), using white fallback`);
        } else {
            console.warn(`Block color not found for type ${blockType}, using white fallback`);
        }
        return new THREE.Color(1.0, 1.0, 1.0);
    }

    /**
     * Update selection mesh to show bordered block
     * @param {number} x - Block X position
     * @param {number} y - Block Y position
     * @param {number} z - Block Z position
     */
    function updateSelectionMesh(x, y, z) {
        // Remove old selection mesh
        clearSelectionMesh();

        // Update the texture uniform in case it wasn't set during initialization
        if (borderedMaterial && noiseTexture) {
            borderedMaterial.uniforms.blockTexture.value = noiseTexture;
        }

        // Create a cube geometry for the selected block
        const geometry = new THREE.BoxGeometry(1.0, 1.0, 1.0);

        // Get the block type at this position to use the correct color
        const blockType = getBlockData(x, y, z);
        const blockColor = getBlockColor(blockType);

        // Set up vertex colors (matching the actual block color from chunk mesh)
        const colors = [];
        for (let i = 0; i < geometry.attributes.position.count; i++) {
            colors.push(blockColor.r, blockColor.g, blockColor.b);
        }
        geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));

        // Create mesh with bordered shader
        selectionMesh = new THREE.Mesh(geometry, borderedMaterial);
        selectionMesh.position.set(x, y, z);
        scene.add(selectionMesh);
    }

    /**
     * Clear the selection mesh
     */
    function clearSelectionMesh() {
        if (selectionMesh) {
            scene.remove(selectionMesh);
            selectionMesh.geometry.dispose();
            selectionMesh = null;
        }
    }

    /**
     * Handle mouse move events
     */
    function onMouseMove(event) {
        updateSelection();
    }

    /**
     * Handle mouse down events
     * @param {MouseEvent} event
     */
    function onMouseDown(event) {
        // Ignore if not left or right click
        if (event.button !== 0 && event.button !== 2) return;

        // Update selection first
        updateSelection();

        if (!selectedBlock) return;

        if (event.button === 0) {
            // Left click - destroy block
            destroyBlock(selectedBlock.x, selectedBlock.y, selectedBlock.z);
        } else if (event.button === 2) {
            // Right click - place block
            // Calculate placement position (adjacent to hit face)
            const placeX = selectedBlock.x + selectedBlock.face.x;
            const placeY = selectedBlock.y + selectedBlock.face.y;
            const placeZ = selectedBlock.z + selectedBlock.face.z;

            placeBlock(placeX, placeY, placeZ);
        }
    }

    /**
     * Destroy a block (set to Air)
     * @param {number} x - Block X position
     * @param {number} y - Block Y position
     * @param {number} z - Block Z position
     */
    function destroyBlock(x, y, z) {
        console.log(`Destroying block at (${x}, ${y}, ${z})`);

        // Update local world data
        setBlockData(x, y, z, 0);

        // Send update to server
        if (window.terraNovaNetwork && window.terraNovaNetwork.isConnected()) {
            window.terraNovaNetwork.sendBlockUpdate(x, y, z, 0); // 0 = Air
        }

        // Clear selection
        selectedBlock = null;
        clearSelectionMesh();
    }

    /**
     * Place a block (Grass block for now)
     * @param {number} x - Block X position
     * @param {number} y - Block Y position
     * @param {number} z - Block Z position
     */
    function placeBlock(x, y, z) {
        // Check if placement position is already occupied
        if (getBlockData(x, y, z) !== 0) {
            console.log(`Cannot place block at (${x}, ${y}, ${z}) - position occupied`);
            return;
        }

        console.log(`Placing block at (${x}, ${y}, ${z})`);

        // Update local world data (1 = Grass)
        setBlockData(x, y, z, 1);

        // Send update to server
        if (window.terraNovaNetwork && window.terraNovaNetwork.isConnected()) {
            window.terraNovaNetwork.sendBlockUpdate(x, y, z, 1); // 1 = Grass
        }
    }

    /**
     * Cleanup and dispose resources
     */
    function dispose() {
        clearSelectionMesh();
        if (borderedMaterial) {
            borderedMaterial.dispose();
        }
        document.removeEventListener('mousedown', onMouseDown);
        document.removeEventListener('mousemove', onMouseMove);
    }

    /**
     * Set the noise texture (can be called after initialization)
     * @param {THREE.Texture} texture - The noise texture
     */
    function setTexture(texture) {
        noiseTexture = texture;
        if (borderedMaterial) {
            borderedMaterial.uniforms.blockTexture.value = texture;
        }
    }

    // Public API
    return {
        initialize: initialize,
        registerChunk: registerChunk,
        unregisterChunk: unregisterChunk,
        setBlockData: setBlockData,
        getBlockData: getBlockData,
        updateSelection: updateSelection,
        setTexture: setTexture,
        setBlockColors: setBlockColors,
        dispose: dispose
    };
})();

// Disable context menu on right-click so we can use it for block placement
document.addEventListener('contextmenu', function(e) {
    e.preventDefault();
}, false);

console.log('Block interaction module loaded');

// Apply pending block colors if they were sent from C# before this module loaded
if (window.terraNova && window.terraNova.applyPendingColors) {
    window.terraNova.applyPendingColors();
}
