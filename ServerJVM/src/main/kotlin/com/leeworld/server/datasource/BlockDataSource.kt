package com.leeworld.server.datasource

import java.util.stream.Stream

interface BlockDataSource {

    fun getBlock(x: Int, y: Int, z: Int): Stream<Short>
}