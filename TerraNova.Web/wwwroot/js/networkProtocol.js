// Terra Nova - Network Protocol Constants
// IMPORTANT: These constants must be kept in sync with TerraNova.Shared/NetworkMessages.cs
// Any changes to NetworkMessages.cs should be reflected here

/**
 * Message type identifiers for network packets
 * Maps to MessageType enum in TerraNova.Shared/NetworkMessages.cs
 */
window.MessageType = {
    // Client -> Server
    CLIENT_CONNECT: 'ClientConnect',
    CHUNK_REQUEST: 'ChunkRequest',
    PLAYER_POSITION: 'PlayerPosition',

    // Server -> Client
    WORLD_DATA: 'WorldData',
    BLOCK_UPDATE: 'BlockUpdate',
    CHUNK_DATA: 'ChunkData',

    // Bidirectional
    DISCONNECT: 'Disconnect'
};

// Freeze the object to prevent modifications
Object.freeze(window.MessageType);

console.log('Network protocol constants loaded (keep in sync with NetworkMessages.cs)');
