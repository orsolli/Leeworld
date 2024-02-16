package com.leeworld.server

import com.leeworld.server.repository.InMemoryBlockRepository
import com.leeworld.server.service.BlockService
import org.junit.jupiter.api.Test
import org.springframework.boot.test.context.SpringBootTest

internal class BlockServiceTests {

    @Test
    fun testGetBlock() {
        val service = BlockService(InMemoryBlockRepository(mutableMapOf("0_0_0" to arrayListOf(
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

    @Test
    fun `can add more detail`() {
        val service = BlockService(InMemoryBlockRepository(mutableMapOf("0_0_0" to arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0100_0000_0000_0000.toShort(),
                        0b0101_0101_0000_0000.toShort()
        ))))
        service.MutateBlock(0, 0, 0, listOf(0,0,0,0,3), false)
        assert(service.GetBlock(0,0,0).count() == 7, { "Mutate did not add more detail" })
    }

    @Test
    fun `can backpropagate`() {
        val service = BlockService(InMemoryBlockRepository(mutableMapOf("0_0_0" to arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0100_0000_0000_0000.toShort(),
                        0b0101_0101_0000_0000.toShort()
        ))))
        service.MutateBlock(0, 0, 0, listOf(0,0,0,0,3), false)
        val newBlock = service.GetBlock(0,0,0)
        assert(newBlock[3] == 0b1000_0000_0000_0000.toShort(), { "Mutate did not backpropogate" })
        assert(newBlock[1] == 0b1000_0000_0000_0000.toShort(), { "Mutate backpropogated too far" })
    }

    @Test
    fun `is indempotent`() {
        val store = arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0100_0000_0000_0000.toShort(),
                        0b0101_0101_0000_0000.toShort()
        )
        val service = BlockService(InMemoryBlockRepository(mutableMapOf("0_0_0" to store)))
        service.MutateBlock(0, 0, 0, listOf(0,0,0,0,3), true)
        assert(service.GetBlock(0,0,0) == store, { "Mutate did not add more detail" })
    }

    @Test
    fun `can remove detail`() {
        val store = arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0100_0000_0000_0000.toShort(),
                        0b0101_0101_0001_0101.toShort()
        )
        val service = BlockService(InMemoryBlockRepository(mutableMapOf("0_0_0" to store)))
        service.MutateBlock(0, 0, 0, listOf(0,0,0,4), true)
        val newBlock = service.GetBlock(0,0,0)
        assert(newBlock.count() == 5, { "Mutate did not reduce detail" })
        assert(newBlock[3] == 0b0100_0000_0000_0000.toShort(), { "Mutate did not backpropogate correctly" })
    }

    @Test
    fun `can remove deep detail`() {
        val store = arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0101_1000_0000_0000.toShort(),
                        0b0101_0101_0001_0101.toShort(),
                        0b0000_1000_0000_0000.toShort(),
                            0b0100_0000_0000_0000.toShort()
        )
        val service = BlockService(InMemoryBlockRepository(mutableMapOf("0_0_0" to store)))
        service.MutateBlock(0, 0, 0, listOf(1,0,2), true)
        val newBlock = service.GetBlock(0,0,0)
        assert(newBlock == arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0101_0100_0000_0000.toShort(),
                        0b0101_0101_0001_0101.toShort()
        ))
    }

    @Test
    fun `can mutate non-detailed node`() {
        val store = arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0100_0000_0000_0000.toShort(),
                        0b0101_0101_0001_0101.toShort()
        )
        val service = BlockService(InMemoryBlockRepository(mutableMapOf("0_0_0" to store)))
        service.MutateBlock(0, 0, 0, listOf(1,0,1), true)
        val newBlock = service.GetBlock(0,0,0)
        assert(newBlock == arrayListOf(
            0b1010_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                0b1000_0000_0000_0000.toShort(),
                    0b1100_0000_0000_0000.toShort(),
                    0b0101_0000_0000_0000.toShort(),
                        0b0101_0101_0001_0101.toShort()
        ))
    }

    @Test
    fun `mutate`() {
        val service = BlockService(InMemoryBlockRepository())
        assert(service.MutateBlock(0, 0, -1, listOf(0), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0001_0101_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(1), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0101_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(2), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0001_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(3,1), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0011_0101_0101.toShort(),0b0100_0101_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(3,3), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0011_0101_0101.toShort(),0b0100_0100_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(3,3), true).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0011_0101_0101.toShort(),0b0100_0101_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(3,3,3), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0011_0101_0101.toShort(),0b0100_0111_0101_0101.toShort(),0b0101_0100_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(3,2,4), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0011_0101_0101.toShort(),0b0100_1111_0101_0101.toShort(),0b0101_0101_0001_0101.toShort(),0b0101_0100_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(3), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0000_0101_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(4), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0000_0001_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(5), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0000_0000_0101.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(6), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0000_0000_0001.toShort()))
        assert(service.MutateBlock(0, 0, -1, listOf(7), false).let { service.GetBlock(0,0,-1) } == arrayListOf(0b0000_0000_0000_0000.toShort()))
    }
}
