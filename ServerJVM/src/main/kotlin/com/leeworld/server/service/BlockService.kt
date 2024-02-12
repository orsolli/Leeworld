package com.leeworld.server.service

import java.util.function.Consumer
import org.springframework.stereotype.Service
import com.leeworld.server.datasource.BlockDataSource

@Service
class BlockService(private val dataSource: BlockDataSource) {
    fun GetBlock(x: Int, y: Int, z: Int, detail: Int = 0): ArrayList<Short> {
        val octreeStream = dataSource.getBlock(x, y, z)
        var nNodesInThisLevel = 1
        var nNodesInNextLevel = 0
        var level = 0
        val result = ArrayList<Short>()
        val stream = octreeStream.peek({ short: Short ->
            result.add(short)
            var mask: Int = 0b10
            while (mask <= 0b1000_0000_0000_0000) {
                if (mask.and(short.toInt()) > 0) {
                    nNodesInNextLevel++
                }
                mask = mask.shl(2)
            }
            nNodesInThisLevel--;
            if (nNodesInThisLevel == 0) {
                nNodesInThisLevel = nNodesInNextLevel
                nNodesInNextLevel = 0
                level++
            }
        }).takeWhile { level != detail || level == 0 }
        val liste = stream.toList()
        println(liste)
        stream.close()
        return result
    }
}
