extends Node
class_name HttpClient

var tls_options: TLSOptions = null

func request(path: String):
	var err = 0
	var http = HTTPClient.new() # Create the Client.
	err = http.connect_to_host("localhost", 8000) # Connect to host/port.
	assert(err == OK) # Make sure connection is OK.
	# Wait until resolved and connected.
	while http.get_status() == HTTPClient.STATUS_CONNECTING or http.get_status() == HTTPClient.STATUS_RESOLVING:
		http.poll()
		#print("Connecting...")
		await get_tree().process_frame

	# Some headers
	var request_headers: PackedStringArray = [
		"User-Agent: Pirulo/1.0 (Godot)",
		"Accept: */*"
	]

	err = http.request(HTTPClient.METHOD_GET, path, request_headers) # Request a page from the site (this one was chunked..)
	assert(err == OK, str(err)) # Make sure all is OK.

	while http.get_status() == HTTPClient.STATUS_REQUESTING:
		# Keep polling for as long as the request is being processed.
		http.poll()
		#print("Requesting...")
		await get_tree().process_frame

	if http.get_status() != HTTPClient.STATUS_BODY and http.get_status() != HTTPClient.STATUS_CONNECTED: # Make sure request finished well.
		return "01"

	if http.has_response():
		var rb = PackedByteArray() # Array that will hold the data.

		while http.get_status() == HTTPClient.STATUS_BODY:
			# While there is body left to be read
			http.poll()
			# Get a chunk.
			var chunk = http.read_response_body_chunk()
			if chunk.size() == 0:
				await get_tree().process_frame
			else:
				rb = rb + chunk # Append to read buffer.
		# Done!

		var body = rb.get_string_from_ascii()
		print(path)
		print(body)
		return body
