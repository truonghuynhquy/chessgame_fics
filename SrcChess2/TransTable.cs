using System;

namespace SrcChess2 {

    /// <summary>Type of transposition entry</summary>
    public enum TransEntryTypeE {
        /// <summary>Exact move value</summary>
        Exact   = 0,
        /// <summary>Alpha cut off value</summary>
        Alpha   = 1,
        /// <summary>Beta cut off value</summary>
        Beta    = 2
    };

    /// <summary>
    /// Implements a transposition table. Transposition table is used to cache already computed board 
    /// </summary>
    public class TransTable {

        /// <summary>Entry in the transposition table</summary>
        private struct TransEntry {
            public long                         m_i64Key;       // 64 bits key compute with Zobrist algorithm. Defined a probably unique board position.
            public int                          m_iGen;         // Generation of the entry
            public ChessBoard.BoardStateMaskE   m_eExtraInfo;   // Board extra info. Defined board extra information
            public int                          m_iDepth;       // Depth of the move (reverse)
            public TransEntryTypeE              m_eType;        // Type of the entry
            public int                          m_iValue;       // Value of the entry
        };
        
        /// <summary>Locking object</summary>
        private readonly object                 m_lock = new object();
        /// <summary>Hashlist of entries</summary>
        private readonly TransEntry[]           m_arrTransEntry;
        /// <summary>Number of cache hit</summary>
        private long                            m_lCacheHit;
        /// <summary>Current generation</summary>
        private int                             m_iGen = 1;      // Start with generation one so empty entry are not considered valid

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="iEntryCount">  Entry count</param>
        public TransTable(int iEntryCount) {
            if (iEntryCount > 2147483647) {
                throw new ArgumentException("Translation Table to big", nameof(iEntryCount));
            }
            m_arrTransEntry = new TransEntry[iEntryCount];
        }

        /// <summary>
        /// Size of the translation table
        /// </summary>
        public int EntryCount => m_arrTransEntry.Length;

        /// <summary>
        /// Gets the entry position for the specified key
        /// </summary>
        /// <param name="zobristKey"></param>
        /// <returns></returns>
        private int GetEntryPos(long zobristKey) => (int)((ulong)zobristKey % (uint)m_arrTransEntry.Length);

        /// <summary>
        /// Record a new entry in the table
        /// </summary>
        /// <param name="i64ZobristKey">    Zobrist key. Probably unique for this board position.</param>
        /// <param name="eExtraInfo">       Extra information about the board not contains in the Zobrist key</param>
        /// <param name="iDepth">           Current depth (reverse)</param>
        /// <param name="iValue">           Board evaluation</param>
        /// <param name="eType">            Type of the entry</param>
        public void RecordEntry(long i64ZobristKey, ChessBoard.BoardStateMaskE eExtraInfo, int iDepth, int iValue, TransEntryTypeE eType) {
            TransEntry  entry;
            int         iEntryPos;

            i64ZobristKey      ^= (int)eExtraInfo;
            iEntryPos           = GetEntryPos(i64ZobristKey);
            entry.m_i64Key      = i64ZobristKey;
            entry.m_iGen        = m_iGen;
            entry.m_eExtraInfo  = eExtraInfo;
            entry.m_iDepth      = iDepth;
            entry.m_iValue      = iValue;
            entry.m_eType       = eType;
            lock(m_lock) {
                m_arrTransEntry[iEntryPos] = entry;
            }
        }

        /// <summary>
        /// Try to find if the current board has already been evaluated
        /// </summary>
        /// <param name="i64ZobristKey">    Zobrist key. Probably unique for this board position.</param>
        /// <param name="eExtraInfo">       Extra information about the board not contains in the Zobrist key</param>
        /// <param name="iDepth">           Current depth (reverse)</param>
        /// <param name="iAlpha">           Alpha cut off</param>
        /// <param name="iBeta">            Beta cut off</param>
        /// <returns>
        /// Int32.MaxValue if no valid value found, else value of the board.
        /// </returns>
        public int ProbeEntry(long i64ZobristKey, ChessBoard.BoardStateMaskE eExtraInfo, int iDepth, int iAlpha, int iBeta) {
            int         iRetVal = Int32.MaxValue;
            int         iEntryPos;
            TransEntry  entry;
            
            i64ZobristKey ^= (int)eExtraInfo;
            iEntryPos      = GetEntryPos(i64ZobristKey);
            lock (m_lock) {
                entry = m_arrTransEntry[iEntryPos];
            }
            if (entry.m_i64Key == i64ZobristKey && entry.m_iGen == m_iGen && entry.m_eExtraInfo == eExtraInfo) {
                if (entry.m_iDepth >= iDepth) {
                    switch(entry.m_eType) {
                    case TransEntryTypeE.Exact:
                        iRetVal = entry.m_iValue;
                        break;
                    case TransEntryTypeE.Alpha:
                        if (entry.m_iValue <= iAlpha) {
                            iRetVal = iAlpha;
                        }
                        break;
                    case TransEntryTypeE.Beta:
                        if (entry.m_iValue >= iBeta) {
                            iRetVal = iBeta;
                        }
                        break;
                    }
                    m_lCacheHit++;
                }
            }
            return(iRetVal);
        }

        /// <summary>
        /// Number of cache hit
        /// </summary>
        public long CacheHit => m_lCacheHit;

        /// <summary>
        /// Reset the cache
        /// </summary>
        public void Reset() {
            m_lCacheHit = 0;
            m_iGen++;
        }
    } // Class TransTable
} // Namespace
