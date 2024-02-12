package com.leeworld.server

import com.leeworld.server.datasource.InMemoryBlockDataSource
import com.leeworld.server.controller.BlockController
import com.leeworld.server.service.BlockService
import org.junit.jupiter.api.Test
import org.springframework.boot.test.context.SpringBootTest

internal class BlockServiceTests {

    @Test
    fun testGetBlock() {

        val service = BlockService(InMemoryBlockDataSource(mapOf("0_0_0" to arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1000_0000_0000_0000.toShort(),
                    0b0100_0000_0000_0000.toShort(),
                        0b0100_0000_0000_0000.toShort()
        ))))
        assert(service.GetBlock(0,0,0,1).count() == 1, { "Detail 1 did not return 1 nodes"})
        assert(service.GetBlock(0,0,0,2).count() == 3, { "Detail 2 did not return 3 nodes"})
        assert(service.GetBlock(0,0,0,3).count() == 5, { "Detail 3 did not return 5 nodes"})
        assert(service.GetBlock(0,0,0).count() == 6, { "Default -1 did not return all"})
    }
}
