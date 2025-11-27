// TerraNova WebGL Renderer
// Custom minimal wrapper optimized for voxel rendering
// Designed to mirror OpenTK API structure for code sharing

window.terraNova = {
  gl: null,
  canvas: null,

  // Shader programs and locations
  program: null,
  buffers: {},

  isRunning: false,
  dotNetHelper: null,
  resizeDotNetHelper: null,
  lastFrameTime: 0,

  /**
   * Initialize WebGL context
   * @param {string} canvasId - ID of the canvas element
   * @returns {boolean} Success
   */
  init: function (canvasId) {
    this.canvas = document.getElementById(canvasId);
    if (!this.canvas) {
      console.error(`Canvas with id '${canvasId}' not found`);
      return false;
    }

    // Try WebGL2 first, fall back to WebGL1
    this.gl = this.canvas.getContext('webgl2') || this.canvas.getContext('webgl');

    if (!this.gl) {
      console.error('WebGL not supported');
      return false;
    }

    // Set initial size
    this.resizeCanvas();

    // Add resize listener
    window.addEventListener('resize', () => this.resizeCanvas());

    // Enable depth testing for 3D rendering
    this.gl.enable(this.gl.DEPTH_TEST);
    this.gl.depthFunc(this.gl.LEQUAL);

    // Enable backface culling for performance
    this.gl.enable(this.gl.CULL_FACE);
    this.gl.cullFace(this.gl.BACK);

    console.log('WebGL initialized successfully');
    return true;
  },

  /**
   * Clear the screen
   * @param {number} r - Red (0-1)
   * @param {number} g - Green (0-1)
   * @param {number} b - Blue (0-1)
   * @param {number} a - Alpha (0-1)
   */
  clear: function (r, g, b, a) {
    const gl = this.gl;
    gl.clearColor(r, g, b, a);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
  },

  /**
   * Compile a shader
   * @param {string} source - Shader source code
   * @param {number} type - gl.VERTEX_SHADER or gl.FRAGMENT_SHADER
   * @returns {WebGLShader|null}
   */
  compileShader: function (source, type) {
    const gl = this.gl;
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);

    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
      console.error('Shader compilation error:', gl.getShaderInfoLog(shader));
      gl.deleteShader(shader);
      return null;
    }

    return shader;
  },

  /**
   * Create shader program from vertex and fragment shader sources
   * @param {string} vertexSource
   * @param {string} fragmentSource
   * @returns {boolean} Success
   */
  createProgram: function (vertexSource, fragmentSource) {
    const gl = this.gl;

    const vertexShader = this.compileShader(vertexSource, gl.VERTEX_SHADER);
    const fragmentShader = this.compileShader(fragmentSource, gl.FRAGMENT_SHADER);

    if (!vertexShader || !fragmentShader) {
      return false;
    }

    this.program = gl.createProgram();
    gl.attachShader(this.program, vertexShader);
    gl.attachShader(this.program, fragmentShader);
    gl.linkProgram(this.program);

    if (!gl.getProgramParameter(this.program, gl.LINK_STATUS)) {
      console.error('Program linking error:', gl.getProgramInfoLog(this.program));
      return false;
    }

    gl.useProgram(this.program);

    // Clean up shaders (they're now part of the program)
    gl.deleteShader(vertexShader);
    gl.deleteShader(fragmentShader);

    console.log('Shader program created successfully');

    this.attribLocations = {
      position: gl.getAttribLocation(this.program, 'aPosition'),
      normal: gl.getAttribLocation(this.program, 'aNormal'),
      textCoord: gl.getAttribLocation(this.program, 'aTexCoord')
    };

    return true;
  },

  /**
   * Create and upload vertex/index buffer for a chunk
   * @param {number} chunkId - Unique chunk identifier
   * @param {Float32Array} vertices - Vertex data
   * @param {Uint16Array} indices - Index data
   */
  uploadChunkMesh: function (chunkId, vertices, indices) {
    const gl = this.gl;

    // Create buffers if they don't exist
    if (!this.buffers[chunkId]) {
      this.buffers[chunkId] = {
        vertex: gl.createBuffer(),
        index: gl.createBuffer(),
        indexCount: 0
      };
    }

    const buffer = this.buffers[chunkId];

    // Upload vertex data
    const vertexData = vertices instanceof Float32Array ? vertices : new Float32Array(vertices);
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer.vertex);
    gl.bufferData(gl.ARRAY_BUFFER, vertexData, gl.STATIC_DRAW);

    // Upload index data
    const indexData = indices instanceof Uint16Array ? indices : new Uint16Array(indices);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, buffer.index);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, indexData, gl.STATIC_DRAW);

    buffer.indexCount = indices.length;
  },

  /**
   * Render a chunk
   * @param {number} chunkId - Chunk to render
   * @param {Float32Array} transform - 4x4 transformation matrix (column-major)
   */
  renderChunk: function (chunkId, transform) {
    const gl = this.gl;
    const buffer = this.buffers[chunkId];

    if (!buffer) {
      console.warn(`Chunk ${chunkId} not uploaded`);
      return;
    }

    // Bind buffers
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer.vertex);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, buffer.index);

    // Set up vertex attributes (position, normal, texcoord, etc.)
    // This will be customized based on your vertex format
    const stride = 8 * 4; // 8 floats per vertex (pos=3, normal=3, uv=2)

    const posLoc = gl.getAttribLocation(this.program, 'aPosition');
    gl.enableVertexAttribArray(posLoc);
    gl.vertexAttribPointer(posLoc, 3, gl.FLOAT, false, stride, 0);

    const normalLoc = gl.getAttribLocation(this.program, 'aNormal');
    if (normalLoc >= 0) {
      gl.enableVertexAttribArray(normalLoc);
      gl.vertexAttribPointer(normalLoc, 3, gl.FLOAT, false, stride, 3 * 4);
    }

    const uvLoc = gl.getAttribLocation(this.program, 'aTexCoord');
    if (uvLoc >= 0) {
      gl.enableVertexAttribArray(uvLoc);
      gl.vertexAttribPointer(uvLoc, 2, gl.FLOAT, false, stride, 6 * 4);
    }

    // Set transformation matrix uniform
    const transformLoc = gl.getUniformLocation(this.program, 'uTransform');
    if (transformLoc) {
      gl.uniformMatrix4fv(transformLoc, false, new Float32Array(transform));
    }

    // Draw the chunk
    gl.drawElements(gl.TRIANGLES, buffer.indexCount, gl.UNSIGNED_SHORT, 0);
  },

  /**
   * Delete a chunk's buffers
   * @param {number} chunkId
   */
  deleteChunk: function (chunkId) {
    const gl = this.gl;
    const buffer = this.buffers[chunkId];

    if (buffer) {
      gl.deleteBuffer(buffer.vertex);
      gl.deleteBuffer(buffer.index);
      delete this.buffers[chunkId];
    }
  },

  /**
   * Get canvas size
   * @returns {{width: number, height: number}}
   */
  getCanvasSize: function () {
    return {
      width: this.canvas.width,
      height: this.canvas.height
    };
  },

  /**
   * Start the render loop
  * @param {object} dotNetHelper - Optional .NET object reference for callbacks
  */
  startRenderLoop: function (dotNetHelper) {
    this.dotNetHelper = dotNetHelper;
    this.isRunning = true;
    this.lastFrameTime = performance.now();
    requestAnimationFrame((time) => this.renderLoop(time));
  },

  /**
   * Stop the render loop
   */
  stopRenderLoop: function () {
    this.isRunning = false;
  },

  /**
   * Main render loop using requestAnimationFrame
   * @param {number} currentTime - Timestamp from requestAnimationFrame (in milliseconds)
   */
  renderLoop: async function (currentTime) {
    if (!this.isRunning) return;

    // Calculate delta time in seconds
    const deltaTime = (currentTime - this.lastFrameTime) / 1000.0;
    this.lastFrameTime = currentTime;

    // Clear screen
    this.clear(0.2, 0.4, 0.8, 1.0);

    // Let C# handle update and rendering
    if (this.dotNetHelper) {
      await this.dotNetHelper.invokeMethodAsync('OnUpdate', deltaTime);
    }

    if (this.isRunning) {
      requestAnimationFrame((time) => this.renderLoop(time));
    }
  },

  /**
   * Resize canvas to fill viewport
  */
  resizeCanvas: function () {
    const displayWidth = this.canvas.clientWidth;
    const displayHeight = this.canvas.clientHeight;

    // Check if canvas internal sizing needs updating
    if (this.canvas.width !== displayWidth
      || this.canvas.height !== displayHeight) {
      this.canvas.width = displayWidth;
      this.canvas.height = displayHeight;

      // Update WebGL viewport
      this.gl.viewport(0, 0, displayWidth, displayHeight);

      console.log(`Canvas resized to ${displayWidth}x${displayHeight}`);
    }
  },

  /**
   * Clean up all WebGL resources and references
   */
  cleanup: function () {
    const gl = this.gl;
    if (!gl) return;

    // Stop render loop
    this.isRunning = false;

    // Delete all chunk buffers
    Object.values(this.buffers).forEach(buffer => {
      gl.deleteBuffer(buffer.vertex);
      gl.deleteBuffer(buffer.index);
    });
    this.buffers = {};

    // Delete shader program
    if (this.program) {
      gl.deleteProgram(this.program);
      this.program = null;
    }

    // Clear .NET references to prevent memory leaks
    this.dotNetHelper = null;
    this.resizeDotNetHelper = null;

    // Remove resize listener if still attached
    if (this._resizeHandler) {
      window.removeEventListener('resize', this._resizeHandler);
      this._resizeHandler = null;
    }

    console.log('WebGL resources cleaned up');
  },

  /**
   * Set up resize listener that notifies .NET
   * @param {object} dotNetHelper - .NET object reference for callbacks
   */
  addResizeListener: function (dotNetHelper) {
    this.resizeDotNetHelper = dotNetHelper;
    this._resizeHandler = () => {
      this.resizeCanvas();
      if (this.resizeDotNetHelper) {
        this.resizeDotNetHelper.invokeMethodAsync('OnResize', this.canvas.width, this.canvas.height);
      }
    };
    window.addEventListener('resize', this._resizeHandler);
  },

  /**
   * Remove resize listener
   */
  removeResizeListener: function () {
    if (this._resizeHandler) {
      window.removeEventListener('resize', this._resizeHandler);
      this._resizeHandler = null;
    }
    this.resizeDotNetHelper = null;
  }
};
