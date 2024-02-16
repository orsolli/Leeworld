package com.leeworld.server

import com.leeworld.server.controller.BlockController
import com.leeworld.server.service.BlockService
import com.leeworld.server.repository.BlockRepository
import org.junit.jupiter.api.Test
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.client.TestRestTemplate
import org.springframework.beans.factory.annotation.Autowired
import org.springframework.http.HttpMethod
import org.springframework.http.RequestEntity
import java.net.URI

@SpringBootTest(
    webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT,
    properties = ["spring.profiles.active=test,inmemory"]
)
internal class BlockControllerTests {

    @Autowired
    lateinit var client: TestRestTemplate

    @Autowired
    lateinit var db: BlockRepository

    @Test
    fun testGetBlock() {
        db.setBlock(0, 0, 0, listOf(
            0b0101_0110_0111_0100.toShort(),
                0b0100_0000_0100_0000.toShort(),
                0b0101_1111_0101_0000.toShort(),
                    0b0101_0101_0101_0100.toShort(),
                    0b0101_0101_0101_0000.toShort(),
        ))
        var body = client.getForEntity<String>("/digg/block/{x}/{y}/{z}/1", String::class.java, "0", "0", "0").body
        assert(body == "0101011001110100", { "should return the first node" })
        body = client.getForEntity<String>("/digg/block/{x}/{y}/{z}/", String::class.java, "0", "0", "0").body
        assert(body == "01010110011101000100000001000000010111110101000001010101010101000101010101010000", { "should return the whole block" })
        assert(body == client.getForEntity<String>("/digg/block/{x}/{y}/{z}/0", String::class.java, "0", "0", "0").body, { "detail 0 should be the same as no detail" })
    }
}
