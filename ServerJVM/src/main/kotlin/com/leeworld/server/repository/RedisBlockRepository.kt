package com.leeworld.server.repository

import java.util.stream.Stream
import org.springframework.stereotype.Repository
import org.springframework.context.annotation.Profile
import com.leeworld.server.repository.BlockRepository
import org.springframework.data.redis.core.RedisTemplate
import org.springframework.data.redis.core.getAndAwait

@Repository
@Profile("redis")
class RedisBlockRepository(private val store: RedisTemplate<String, List<Short>>) : BlockRepository {

    override fun getBlock(x: Int, y: Int, z: Int): Stream<Short> {
        val position = "${x}_${y}_${z}"
        val octree = store.opsForValue().get(position);
        if (octree != null && octree.size > 0) return octree.stream()
        if ('-' in position) return arrayListOf(0b0101_0101_0101_0101.toShort()).stream()
        else return arrayListOf(0b0000_0000_0000_0000.toShort()).stream()
    }

    override fun setBlock(x: Int, y: Int, z: Int, newOctree: List<Short>): Unit {
        val blockPosition = "${x}_${y}_${z}"
        store.opsForValue().set(blockPosition, newOctree)
    }
}
