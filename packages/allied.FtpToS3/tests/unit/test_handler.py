import json
import pytest
from first_function import app



@pytest.fixture()
def lambda_event():
    """ Generates Lambda Event"""

    return { "foo": "bar" }

def test_lambda_handler(lambda_event):
    ret = app.lambda_handler(lambda_event, "")
    assert ret == {'hello': 'world'}

