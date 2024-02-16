package com.leeworld.server.repository

import java.util.stream.Stream

interface BlockRepository {

    fun getBlock(x: Int, y: Int, z: Int): Stream<UShort>
    fun setBlock(x: Int, y: Int, z: Int, newOctree: List<UShort>): Unit
}