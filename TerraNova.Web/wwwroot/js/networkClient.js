// Terra Nova - WebSocket Network Client
// Handles connection to the game server via WebSocket

window.terraNovaNetwork = (function() {
    let ws = null;
    let connected = false;
    let worldDataCallback = null;
    let blockUpdateCallback = null;
    let chunkDataCallback = null;

    /**
     * Connect to the game server
     * @param {string} serverUrl - WebSocket server URL (e.g., "ws://localhost:5000/ws")
     * @param {function} onWorldData - Callback when world data is received
     * @param {function} onBlockUpdate - Callback when block update is received
     * @param {function} onChunkData - Callback when chunk data is received
     */
    function connect(serverUrl, onWorldData, onBlockUpdate, onChunkData) {
        console.log(`Connecting to server: ${serverUrl}`);

        worldDataCallback = onWorldData;
        blockUpdateCallback = onBlockUpdate;
        chunkDataCallback = onChunkData;

        ws = new WebSocket(serverUrl);

        ws.onopen = () => {
            console.log('WebSocket connected');
            connected = true;

            // Send client connect message
            const connectMsg = {
                type: MessageType.CLIENT_CONNECT,
                playerName: 'WebPlayer'
            };
            sendMessage(connectMsg);
        };

        ws.onmessage = (event) => {
            try {
                const message = JSON.parse(event.data);
                handleMessage(message);
            } catch (error) {
                console.error('Error parsing message:', error);
            }
        };

        ws.onerror = (error) => {
            console.error('WebSocket error:', error);
        };

        ws.onclose = () => {
            console.log('WebSocket disconnected');
            connected = false;
        };
    }

    /**
     * Disconnect from the server
     */
    function disconnect() {
        if (ws) {
            ws.close();
            ws = null;
            connected = false;
        }
    }

    /**
     * Send a message to the server
     * @param {object} message - Message object to send
     */
    function sendMessage(message) {
        if (ws && ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify(message));
        } else {
            console.warn('Cannot send message: WebSocket not connected');
        }
    }

    /**
     * Handle incoming message from server
     * @param {object} message - Parsed message object
     */
    function handleMessage(message) {
        switch (message.type) {
            case MessageType.WORLD_DATA:
                console.log(`Received world data: ${message.blocks.length} blocks`);
                if (worldDataCallback) {
                    worldDataCallback(message.blocks);
                }
                break;

            case MessageType.BLOCK_UPDATE:
                console.log(`Block update: (${message.x}, ${message.y}, ${message.z}) = ${message.blockType}`);

                // Update block interaction system's world data
                if (window.terraNovaBlockInteraction) {
                    window.terraNovaBlockInteraction.setBlockData(message.x, message.y, message.z, message.blockType);
                    window.terraNovaBlockInteraction.updateSelection(); // Refresh selection
                }

                if (blockUpdateCallback) {
                    blockUpdateCallback(message.x, message.y, message.z, message.blockType);
                }
                break;

            case MessageType.CHUNK_DATA:
                console.log(`Received chunk data: (${message.chunkX}, ${message.chunkZ}) with ${message.blocks.length} blocks`);
                if (chunkDataCallback) {
                    chunkDataCallback(message.chunkX, message.chunkZ, message.blocks);
                }
                break;

            default:
                console.warn('Unknown message type:', message.type);
        }
    }

    /**
     * Send a block update to the server
     * @param {number} x - Block X position
     * @param {number} y - Block Y position
     * @param {number} z - Block Z position
     * @param {number} blockType - Block type (0 = Air, 1 = Grass, etc.)
     */
    function sendBlockUpdate(x, y, z, blockType) {
        const message = {
            type: MessageType.BLOCK_UPDATE,
            x: x,
            y: y,
            z: z,
            blockType: blockType
        };
        sendMessage(message);
    }

    /**
     * Request specific chunks from the server
     * @param {Array<{x: number, z: number}>} chunkPositions - Array of chunk positions to request
     */
    function requestChunks(chunkPositions) {
        const message = {
            type: MessageType.CHUNK_REQUEST,
            chunkPositions: chunkPositions
        };
        sendMessage(message);
        console.log(`Requested ${chunkPositions.length} chunks from server`);
    }

    /**
     * Check if connected to server
     */
    function isConnected() {
        return connected;
    }

    // Expose public API
    return {
        connect: connect,
        disconnect: disconnect,
        sendBlockUpdate: sendBlockUpdate,
        requestChunks: requestChunks,
        isConnected: isConnected
    };
})();
